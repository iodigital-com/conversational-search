import {useEffect, useState} from "preact/hooks";
import {Configuration, HoldConversationReference, HoldConversationResponse, Message} from "@/types/types";
import styles from "@/components/Chat.module.css";
import {JSX} from "preact";
import {getConversationContext, holdConversation, startConversation} from "@/services/ConversationApiService.ts";
import {camelCaseReviver} from "@/utils/JsonUtils.ts";
import {findLastIndex} from "@/utils/ArrayUtils.ts";

const MAX_QUESTION_LENGTH = 100;

async function initializeConversation(configuration: Configuration) {
    const response = await startConversation(configuration);
    return response?.conversationId;
}

export const Chat = (configuration: Configuration) => {
    const abortController = new AbortController();
    const [conversationId, setConversationId] = useState<string | undefined>(undefined);
    const [messages, setMessages] = useState<Message[]>([]);
    const [references, setReferences] = useState<HoldConversationReference[]>([]);
    const [contextVariables, setContextVariables] = useState<Map<string, string>>();
    const contextVariablesAsArray = [...contextVariables?.entries() ?? []];
    const [input, setInput] = useState<string>("");

    const [loading, setLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);

    useEffect(() => {
        (async () => {
            if (!conversationId) {
                const createdConversationId = await initializeConversation(configuration);
                const conversationContextVariables = await getConversationContext(configuration);
                if (createdConversationId) {
                    setConversationId(createdConversationId);
                    setContextVariables(new Map(conversationContextVariables?.variables.map((obj: string) => [obj, ""])));
                    setLoading(false);
                }
            }
        })();
    }, [conversationId])
    const updateMessageChunk = (value: string) => {
        return Promise.resolve().then(_ => {
            const jsonObjects = value.split('}{'); // split if multiple json objects appended

            let tempAnswer = "";
            jsonObjects.forEach((json, index, array) => {
                if (array.length > 1) {
                    // Add missing `{` at the beginning if necessary
                    if (index !== 0) {
                        json = `{${json}`;
                    }

                    // Add missing `}` at the end if necessary
                    if (index !== jsonObjects.length - 1) {
                        json = `${json}}`;
                    }
                }

                const response: HoldConversationResponse = JSON.parse(json, camelCaseReviver);
                tempAnswer = response.response.answer;

                console.log(tempAnswer);
                setMessages(existing => {
                    let newArr = [...existing];
                    let idx = findLastIndex(newArr, message => message.role == "assistant");
                    let matching = newArr[idx];
                    matching.content += tempAnswer;
                    return newArr;
                });
                setReferences(response.references);
            });
        })

    };

    const onSubmit = async (e: JSX.TargetedEvent) => {
        e.preventDefault();

        const question = {role: "user" as const, content: input};
        setMessages([...messages, question]);
        setLoading(true);
        setHasError(false);

        try {
            const blankAnswer = {role: "assistant" as const, content: ""};

            setMessages(prevState => {
                const newMessages = prevState.slice();
                newMessages.push(blankAnswer);
                return newMessages;
            });

            await holdConversation(conversationId!,
                input,
                Object.fromEntries(contextVariables ?? new Map()),
                updateMessageChunk,
                abortController.signal,
                configuration);
        } catch (e) {
            console.error(e);
            setHasError(true);
        } finally {
            setLoading(false);
            setInput("");
        }
    };

    const onReset = () => {
        setMessages([]);
        setLoading(true);
        setHasError(false);
        setInput("");
        setConversationId(undefined);
        setReferences([]);
    }

    const onContextVariableChange = (e: Omit<Event, "currentTarget"> & {
        readonly currentTarget: HTMLInputElement
    }, key: string) => {
        setContextVariables(prevState => {
            const newContextVariables = new Map(prevState);
            const target = e.target as HTMLInputElement;
            newContextVariables.set(key, target.value);
            return newContextVariables;
        });
    };

    return (
        <section>
            <h2>Chat</h2>
            {messages.map((m, i) => (
                <div class={styles.message} key={i}>
                    <div class={styles.role}>{m.role}</div>
                    <pre class={styles.messageContent} dangerouslySetInnerHTML={{__html: m.content}}></pre>
                </div>
            ))}

            {hasError && (
                <div class={styles.error}>An error occurred. Please retry.</div>
            )}

            <form onSubmit={onSubmit}>
                <input
                    class={styles.input}
                    placeholder={`Ask me anything`}
                    type="text"
                    value={input}
                    onInput={(e) => setInput((e.target as any)?.value ?? "")}
                    maxLength={MAX_QUESTION_LENGTH}
                    disabled={loading}
                />
                <div>
                    <button class={styles.submit} type="submit" disabled={loading}>Submit</button>
                    <button class={styles.reset} onClick={onReset} disabled={loading}>Reset</button>
                </div>
            </form>
            <section>
                <h2>Context Variables</h2>
                {contextVariablesAsArray.map(([key, value]) => (<div>
                        <label for={key}>
                            {key}
                        </label>
                        <input
                            name={key}
                            key={key}
                            value={value || ''}
                            onChange={e => onContextVariableChange(e, key)}
                        />
                    </div>
                ))}
            </section>
            <section>
                <h2>References</h2>
                {references.map((r, i) => (
                    <div class={styles.references} key={i}>
                        <div class={styles.role}>
                            <a href={r.url} target={"_blank"} rel={"noreferrer norel"}>{r.title} [{r.index}] </a>
                        </div>
                    </div>
                ))}
            </section>
        </section>
    );
};
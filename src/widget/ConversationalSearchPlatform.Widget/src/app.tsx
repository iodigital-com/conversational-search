import {Chat} from "@/components/Chat.tsx";
import './index.css'

export function App(props: any) {
// TODO error handling instead of defaults
    console.log(props);
    return <Chat apiUrl={props.apiUrl} apiKey={props.apiKey}/>
}

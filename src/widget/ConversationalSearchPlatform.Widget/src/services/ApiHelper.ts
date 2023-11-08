interface HttpResponse<T> extends Response {
    parsedBody?: T;
}

export async function http<T>(
    request: RequestInfo
): Promise<HttpResponse<T>> {
    const response: HttpResponse<T> = await fetch(
        request
    );

    try {
        // may error if there is no body
        response.parsedBody = await response.json();
    } catch (ex) {
    }

    if (!response.ok) {
        throw new Error(response.statusText);
    }
    return response;
}

export async function httpStreaming<T>(
    request: RequestInfo,
    callback: (callbackValue: string) => Promise<void>
): Promise<void> {
    const response: HttpResponse<T> = await fetch(
        request
    );

    if (!response.ok) {
        throw new Error(response.statusText);
    }
    if (response && (response.status >= 200 && response.status < 300 && response.body)) {
        await streamResponse(response, callback);
    }
}

export async function get<T>(
    path: string,
    args: RequestInit = {method: "get"}
): Promise<HttpResponse<T>> {
    return await http<T>(new Request(path, args));
}

export async function post<T>(
    path: string,
    body: any,
    args: RequestInit = {method: "post", body: JSON.stringify(body)}
): Promise<HttpResponse<T>> {
    return await http<T>(new Request(path, args));
}

export async function put<T>(
    path: string,
    body: any,
    args: RequestInit = {method: "put", body: JSON.stringify(body)}
): Promise<HttpResponse<T>> {
    return await http<T>(new Request(path, args));
}

async function streamResponse(response: Response, callback: (callbackValue: string) => Promise<void>) {
    const reader = response.body?.getReader();
    if (!reader) {
        throw new Error('Failed to read response');
    }
    const decoder = new TextDecoder();

    while (true) {
        const {done, value} = await reader.read();
        if (done) break;
        if (!value) continue;

        const chunk = decoder.decode(value);
        await callback(chunk);
    }
    reader.releaseLock();
}
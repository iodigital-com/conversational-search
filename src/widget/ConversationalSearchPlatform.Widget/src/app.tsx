import {Chat} from "@/components/Chat.tsx";

const BASE_URL = "http://localhost:5199/api/v1"; //TODO get prop from root 
// iodigital demo
// const API_KEY = "4903E29F-D633-4A4C-9065-FE3DD8F27E40";
//polestar
const API_KEY = "D2FA78CE-3185-458E-964F-8FD0052B4330";

export function App() {
// TODO make
    return <Chat apiUrl={BASE_URL} apiKey={API_KEY}/>
}

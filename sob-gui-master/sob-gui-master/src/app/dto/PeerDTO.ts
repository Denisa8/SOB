import { Guid } from "guid-typescript";
import { FileDTO } from "./FileDTO";

export class PeerDTO {
    id: string;
    available: boolean;
    correctSendData: boolean;
    bannedPeers: Guid[];
    files: FileDTO[];


    constructor(id: string, available: boolean, correctSendData: boolean, bannedPeers: Guid[], files: FileDTO[]) {
        this.id = id;
        this.available = available;
        this.correctSendData = correctSendData;
        this.bannedPeers = bannedPeers;
        this.files = files;
    }

}
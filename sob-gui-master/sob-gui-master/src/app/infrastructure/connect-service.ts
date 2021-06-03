import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Guid } from "guid-typescript";
import { Observable } from "rxjs";
import { PeerDTO } from "../dto/PeerDTO";

@Injectable({
    providedIn: 'root',
})
export class ConnectService {
    SERVER_URL = "https://localhost:44389"
    constructor(private httpClient: HttpClient) {}

    public getAllPeers(): Observable<PeerDTO[]> {
        console.log('getAllPeers()');

        const url = '/tracker/peer-list';
        return this.httpClient.get<PeerDTO[]>(this.SERVER_URL + url);
    }

    public changeAvailable(id: string, available: boolean) {
        console.log('changeAvailable()');
        console.log(id);
        console.log(available);

        const url = '/tracker/change-available/';
        this.httpClient.get(this.SERVER_URL + url + id.toString() + '/' + available).subscribe(result => {
		console.log(result);
    });
    }

    public sendBadData(id: string, correctdata: boolean) {
        console.log('sendBadData()');
        console.log(id);
        console.log(correctdata);

        const url = '/tracker/change-send-data/';
        this.httpClient.get(this.SERVER_URL + url + id + '/' + correctdata);
    }
}
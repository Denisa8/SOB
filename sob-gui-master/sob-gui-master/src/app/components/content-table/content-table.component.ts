import { Component } from '@angular/core';
import { PeerDTO } from '../../dto/PeerDTO';
import { ConnectService } from '../../infrastructure/connect-service';
import { Guid } from "guid-typescript";
import { FileDTO } from '../../dto/FileDTO';

@Component({
  selector: 'app-content-table',
  templateUrl: './content-table.component.html'
})
export class ContentTableComponent {

  expandSet = new Set<string>();
	private interval: any;
  onExpandChange(id: string, checked: boolean): void {
    if (checked) {
      this.expandSet.add(id);
    } else {
      this.expandSet.delete(id);
    }
  }

  /* Mocked data */
  // peers: PeerDTO[] = [
  //   {
  //     id: Guid.create(),
  //     Available: false,
  //     CorrectSendData: false,
  //     BannedPeers: [Guid.create(), Guid.create()],
  //     Files: [
  //       new FileDTO('plik1', 80),
  //       new FileDTO('plik2', 20)
  //     ]
  //   },
  //   {
  //     id: Guid.create(),
  //     Available: true,
  //     CorrectSendData: true,
  //     BannedPeers: [Guid.create(), Guid.create()],
  //     Files: [
  //       new FileDTO('plik1', 30),
  //       new FileDTO('plik2', 70),
  //       new FileDTO('plik3', 50)
  //     ]
  //   },
  //   {
  //     id: Guid.create(),
  //     Available: false,
  //     CorrectSendData: false,
  //     BannedPeers: [Guid.create(), Guid.create(), Guid.create()],
  //     Files: [
  //       new FileDTO('plik1', 80),
  //       new FileDTO('plik2', 20)
  //     ]
  //   }
  // ]

  peers: PeerDTO[] = [];

  constructor(public connectService: ConnectService) {}

  ngOnInit(): void {

    setTimeout(() => {
      this.interval = setInterval(() => {
		  this.connectService.getAllPeers().subscribe(
        value => {
          if (value === null) {
            console.log('value is null');
            return;
          } 
          this.peers = value;
        },
        error => {
          console.log('Internal Server Error - 500');
        }
      );  
    }, 1000);
  });}

  changeAvailable(event: Event, data: PeerDTO) {
    let elementid: string = (event.target as Element).id; 
    
    this.connectService.changeAvailable(data.id, data.available);
	data.available = !data.available;
  }

  sendBadData(event: Event) {
    let elementid: string = (event.target as Element).id;
    let correctdata: boolean = this.getCorrectSendDataByid(elementid);

    this.connectService.sendBadData(elementid, correctdata);
  }

  getAvailableByid(elementid: string): boolean {
    for (let i = 0; i < this.peers.length; i++) {
      if (this.peers[i].id.toString() === elementid) {
        return this.peers[i].available;
      }
    }

    return false;
  }

  getCorrectSendDataByid(elementid: string): boolean {
    for (let i = 0; i < this.peers.length; i++) {
      if (this.peers[i].id.toString() === elementid) {
        return this.peers[i].correctSendData;
      }
    }

    return false;
  }
}

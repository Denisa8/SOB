export class FileDTO {
    filename: string;
    progress: number;

    constructor(Filename: string, Progress: number) {
        this.filename = Filename;
        this.progress = Progress;
    }
}
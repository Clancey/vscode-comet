import { Subject } from "rxjs";
import { first } from 'rxjs/operators';

export class ObservableValue<T> extends Subject<T> {
    public value: T = null;

    public next(value?: T) {
        this.value = value;
        super.next(value);
    }

    public toPromise<T>() {
        if (this.value) {
            return Promise.resolve(this.value);
        }
        else {
            return super.pipe(first()).toPromise();
        }
    }
}
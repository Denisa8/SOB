import { fakeAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { ContentTableComponent } from './content-table.component';

describe('ContentTableComponent', () => {
  let component: ContentTableComponent;
  let fixture: ComponentFixture<ContentTableComponent>;

  beforeEach(fakeAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ContentTableComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ContentTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }));

  it('should compile', () => {
    expect(component).toBeTruthy();
  });
});

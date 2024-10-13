import {
    ChangeDetectorRef,
    Directive,
    Input,
    IterableChangeRecord,
    IterableDiffer,
    IterableDiffers,
    TemplateRef,
    ViewContainerRef,
    ViewRef
} from '@angular/core';
import { ImportItem } from '../../core/models/import-item/import-item.model';

@Directive({
    selector: '[ngxtFor]',
    standalone: true
})
export class NgxtForDirective {
    private items: any[] = [];
    private viewRefsMap: Map<any, ViewRef> = new Map<any, ViewRef>();
    private _difference: IterableDiffer<any> | undefined;
    private renderedItems = new Set<any>();

    constructor(
        private templateRef: TemplateRef<any>,
        private viewContainer: ViewContainerRef,
        private differs: IterableDiffers,
        private cdr: ChangeDetectorRef
    ) {
        this._difference = this.differs.find([]).create();
    }

    @Input('ngxtForOf')
    public set ngxtForOf(items: any) {
        this.items = items;
        if (items) {
            this._difference = this.differs.find(items).create();
            this.cdr.detectChanges();
        }
    }

    @Input('ngxtForItemsAtOnce')
    public itemsAtOnce: number = 10;

    @Input('ngxtForIntervalLength')
    public intervalLength: number = 50;

    @Input('ngxtForTrackBy')
    public trackByFn: (index: number, item: ImportItem) => any = (index, item) => item;

    private modifiedTrackByFn(index: number | null, item: ImportItem): any {
        return index !== null ? this.trackByFn(index, item) : item;
    }

    public ngDoCheck(): void {
        if (this._difference) {
            const changes = this._difference.diff(this.items);
            if (changes) {
                const itemsAdded: any[] = [];
                const itemsRemoved: any[] = [];

                let countAddedItems = 0;
                changes.forEachAddedItem(item => {
                    itemsAdded.push(item);
                    countAddedItems++;
                });

                changes.forEachRemovedItem(item => {
                      itemsRemoved.push(item);
                });

                this.progressiveRender(itemsAdded);
                this.removeItems(itemsRemoved);

                changes.forEachMovedItem(item => {
                    const mapView = this.viewRefsMap.get(item.item) as ViewRef;
                    if (mapView) {
                        const viewIndex = this.viewContainer.indexOf(mapView);
                        if (viewIndex !== -1) {
                            this.viewContainer.move(mapView, item.currentIndex || 0);
                        }
                    }
                });

                changes.forEachIdentityChange(item => {
                    const mapView = this.viewRefsMap.get(item.item) as ViewRef;
                    if (mapView) {
                        const viewIndex = this.viewContainer.indexOf(mapView);
                        if (viewIndex !== -1) {
                            this.viewContainer.remove(viewIndex);
                        }
                        this.viewRefsMap.delete(item.item);
                        this.renderedItems.delete(this.modifiedTrackByFn(item.currentIndex, item.item));

                        const embeddedView = this.viewContainer.createEmbeddedView(
                            this.templateRef,
                            {
                                $implicit: item.item,
                                index: item.currentIndex
                            },
                            item.currentIndex || 0
                        );
                        this.viewRefsMap.set(item.item, embeddedView);
                        this.renderedItems.add(this.modifiedTrackByFn(item.currentIndex, item.item));
                    }
                });
            }
        }
    }

    private progressiveRender(items: IterableChangeRecord<any>[]) {
        let start = 0;
        let end = start + this.itemsAtOnce;
        if (end > items.length) {
            end = items.length;
        }
        this.renderItems(items, start, end);

        const renderNextBatch = () => {
            start = end;
            end = start + this.itemsAtOnce;
            if (end > items.length) {
                end = items.length;
            }
            this.renderItems(items, start, end);
            if (start < items.length) {
                requestAnimationFrame(renderNextBatch);
            }
        };

        requestAnimationFrame(renderNextBatch);
    }

    private renderItems(
        items: IterableChangeRecord<any>[],
        start: number,
        end: number
    ) {
        items.slice(start, end).forEach(item => {
            const trackById = this.modifiedTrackByFn(item.currentIndex, item.item);
            if (!this.renderedItems.has(trackById)) {
                const index = item.currentIndex || 0;
                const validIndex = Math.min(index, this.viewContainer.length);
                const embeddedView = this.viewContainer.createEmbeddedView(
                    this.templateRef,
                    {
                        $implicit: item.item,
                        index: item.currentIndex
                    },
                    validIndex
                );
                this.viewRefsMap.set(item.item, embeddedView);
                this.renderedItems.add(trackById);
            }
        });
    }

    private removeItems(items: IterableChangeRecord<any>[]) {
        items.forEach(item => {
            const mapView = this.viewRefsMap.get(item.item) as ViewRef;
            if (mapView) {
                const viewIndex = this.viewContainer.indexOf(mapView);
                if (viewIndex !== -1) {
                    this.viewContainer.remove(viewIndex);
                }
                this.viewRefsMap.delete(item.item);
                this.renderedItems.delete(this.modifiedTrackByFn(item.currentIndex, item.item));
            }
        });
    }
}

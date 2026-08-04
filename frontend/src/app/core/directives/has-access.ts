import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';

@Directive({
  selector: '[appHasAccess]',
})
export class HasAccess {
  appHasAccess = input(false);

  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainerRef = inject(ViewContainerRef);
  private hasView = false;

  constructor() {
    effect(() => this.updateView(this.appHasAccess()));
  }

  private updateView(hasAccess: boolean): void {
    if (hasAccess && !this.hasView) {
      this.viewContainerRef.createEmbeddedView(this.templateRef);
      this.hasView = true;
      return;
    }

    if (!hasAccess && this.hasView) {
      this.viewContainerRef.clear();
      this.hasView = false;
    }
  }
}

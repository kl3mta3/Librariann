import {ChangeDetectionStrategy, Component, effect, input, model, signal} from '@angular/core';

let nextId = 0;

/**
 * A lightweight, collapsible section header for the sidenav (Libraries, Browse, Smart Filters, Collections,
 * Server Tools). Reuses the same CSS Grid `0fr -> 1fr` collapse technique as {@link AccordionComponent}, but is
 * intentionally its own component rather than reusing that one directly: the accordion is a bordered/padded
 * card tile meant for settings-style content, while this needs to sit flush inside a flat nav list with a much
 * smaller header (just a muted label + chevron), and needs a "rail mode" the accordion has no concept of - when
 * the whole sidenav is collapsed to an icon-only rail, the group header disappears entirely and every child item
 * must always render regardless of the group's stored open/closed state, so icons stay reachable.
 */
@Component({
  selector: 'app-side-nav-group',
  templateUrl: './side-nav-group.component.html',
  styleUrls: ['./side-nav-group.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.is-open]': 'open()',
    '[class.is-fully-open]': 'fullyOpen()',
    '[class.rail-mode]': 'railMode()',
  }
})
export class SideNavGroupComponent {
  /**
   * Pre-translated header label (e.g. "Libraries").
   */
  label = input.required<string>();
  /**
   * Whether the group is expanded. Two-way bindable so the parent can persist it.
   */
  open = model<boolean>(false);
  /**
   * True when the whole sidenav is collapsed to an icon-only rail - hides the header and forces
   * the body to always be visible so icons stay reachable regardless of the stored open state.
   */
  railMode = input<boolean>(false);
  /**
   * Optional item-count badge shown next to the label.
   */
  count = input<number | null>(null);

  protected readonly bodyId = `side-nav-group-body-${nextId++}`;
  /**
   * True only once the open animation has finished. Matches AccordionComponent's technique -
   * switches the body back to `overflow: visible` so dropdown menus (e.g. app-card-actionables)
   * projected inside library rows aren't clipped by the collapse wrapper mid-animation.
   */
  protected readonly fullyOpen = signal(false);

  constructor() {
    effect((onCleanup) => {
      if (this.open() || this.railMode()) {
        const timer = setTimeout(() => this.fullyOpen.set(true), 300);
        onCleanup(() => clearTimeout(timer));
      } else {
        this.fullyOpen.set(false);
      }
    });
  }

  toggle() {
    if (this.railMode()) return;
    this.open.update(v => !v);
  }
}

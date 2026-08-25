import {ChangeDetectionStrategy, Component, computed, inject, input, output} from '@angular/core';
import {APP_BASE_HREF, NgClass} from '@angular/common';
import {SideNavStream} from "../../../_models/sidenav/sidenav-stream";
import {StreamNamePipe} from "../../../_pipes/stream-name.pipe";
import {TranslocoDirective} from "@jsverse/transloco";
import {SideNavStreamType} from "../../../_models/sidenav/sidenav-stream-type.enum";
import {FilterUtilitiesService} from "../../../shared/_services/filter-utilities.service";

@Component({
  selector: 'app-sidenav-stream-list-item',
  imports: [StreamNamePipe, TranslocoDirective, NgClass],
  templateUrl: './sidenav-stream-list-item.component.html',
  styleUrls: ['./sidenav-stream-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SidenavStreamListItemComponent {
  item = input.required<SideNavStream>();
  position = input.required<number>();

  hide = output<SideNavStream>();
  delete = output<SideNavStream>();

  externalUrl = computed(() => {
    const source = this.item().externalSource;

    if (!source)
      return '';

    return source.launchApiPath
      ? this.baseUrl.replace(/\/?$/, '/') + source.launchApiPath.replace(/^\//, '')
      : source.host;
  });
  loadFilterUrl = computed(() => {
    return this.baseUrl + FilterUtilitiesService.getFilterLink(this.item().entityType, this.item().smartFilterEncoded ?? '');
  });

  protected readonly SideNavStreamType = SideNavStreamType;
  protected readonly baseUrl = inject(APP_BASE_HREF);
}

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/** Small line-icon glyph, one per guidance topic. Built from plain SVG primitives (rect/circle/line/polyline/polygon) so nothing depends on hand-authored curve paths. */
@Component({
  selector: 'app-topic-icon',
  standalone: true,
  imports: [CommonModule],
  template: `
    <svg [attr.width]="size" [attr.height]="size" viewBox="0 0 24 24" fill="none" [attr.stroke]="color" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
      <ng-container [ngSwitch]="kind">
        <ng-container *ngSwitchCase="'recruiting'">
          <rect x="3" y="7" width="18" height="12" rx="2"/>
          <path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
        </ng-container>
        <ng-container *ngSwitchCase="'compensation'">
          <circle cx="12" cy="12" r="8"/>
          <line x1="12" y1="8" x2="12" y2="16"/>
          <line x1="9" y1="10" x2="15" y2="10"/>
          <line x1="9" y1="14" x2="15" y2="14"/>
        </ng-container>
        <ng-container *ngSwitchCase="'engagement'">
          <path d="M4 5h16v10H8l-4 4z"/>
        </ng-container>
        <ng-container *ngSwitchCase="'retention'">
          <path d="M12 3l7 3v6c0 5-3.5 7.5-7 9-3.5-1.5-7-4-7-9V6z"/>
        </ng-container>
        <ng-container *ngSwitchCase="'performance'">
          <circle cx="12" cy="12" r="8"/>
          <circle cx="12" cy="12" r="4.3"/>
          <circle cx="12" cy="12" r="1.1" [attr.fill]="color" stroke="none"/>
        </ng-container>
        <ng-container *ngSwitchCase="'policy'">
          <rect x="5" y="3" width="14" height="18" rx="1.5"/>
          <line x1="8" y1="8" x2="16" y2="8"/>
          <line x1="8" y1="12" x2="16" y2="12"/>
          <line x1="8" y1="16" x2="13" y2="16"/>
        </ng-container>
        <ng-container *ngSwitchCase="'payroll'">
          <rect x="3" y="6" width="18" height="13" rx="2"/>
          <path d="M3 10h18"/>
          <circle cx="17" cy="14.2" r="1.2" [attr.fill]="color" stroke="none"/>
        </ng-container>
        <ng-container *ngSwitchCase="'teamhealth'">
          <polyline points="3,12 8,12 10,6 14,18 16,12 21,12"/>
        </ng-container>
        <ng-container *ngSwitchCase="'onboarding'">
          <circle cx="12" cy="12" r="9"/>
          <polygon points="12,7 14.5,12 12,17 9.5,12"/>
        </ng-container>
      </ng-container>
    </svg>
  `
})
export class TopicIconComponent {
  @Input() kind = '';
  @Input() size = 16;
  @Input() color = 'currentColor';
}

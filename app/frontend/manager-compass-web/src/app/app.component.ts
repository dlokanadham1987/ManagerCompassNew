import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ApiService } from './services/api.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  managerName = 'Lokanadham Dasamukha';
  escalationsThisWeek: number | null = null;
  leadershipPulseColor: string | null = null;
  leadershipPulseLabel: string | null = null;

  constructor(private readonly api: ApiService) {}

  get managerInitials(): string {
    return this.managerName
      .split(' ')
      .filter(Boolean)
      .map(w => w[0])
      .join('')
      .toUpperCase();
  }

  ngOnInit(): void {
    this.api.getAdoptionMetrics().subscribe(m => (this.escalationsThisWeek = m.escalationsRoutedThisWeek));
    this.api.getLeadershipCard().subscribe(c => {
      this.leadershipPulseColor = c.pulseColor;
      this.leadershipPulseLabel = c.pulseLabel;
    });
  }
}

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { LeadershipCard } from '../../models/leadership.model';

@Component({
  selector: 'app-leadership',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './leadership.component.html'
})
export class LeadershipComponent implements OnInit {
  card: LeadershipCard | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getLeadershipCard().subscribe(c => (this.card = c));
  }

  print(): void {
    window.print();
  }

  initials(name: string): string {
    return name
      .split(' ')
      .filter(w => w.length && w[0] !== '[')
      .map(w => w[0])
      .join('')
      .slice(0, 2)
      .toUpperCase() || 'MC';
  }
}

import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { CommentTargetType, Team, TeamComment, TeamMember } from '../../models/team-notes.model';

type ModalKind = 'none' | 'view' | 'form' | 'delete';

const TITLE_MAX = 120;
const DESCRIPTION_MAX = 2000;

@Component({
  selector: 'app-team-notes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './team-notes.component.html',
  styleUrl: './team-notes.component.css'
})
export class TeamNotesComponent implements OnInit {
  readonly titleMax = TITLE_MAX;
  readonly descriptionMax = DESCRIPTION_MAX;

  teams: Team[] = [];
  comments: TeamComment[] = [];
  loading = true;
  error = '';
  search = '';
  collapsedTeams = new Set<string>();

  modal: ModalKind = 'none';
  activeComment: TeamComment | null = null; // the note being viewed / edited / deleted
  formTarget = ''; // "team:<id>" or "member:<id>"
  draftTitle = '';
  draftDescription = '';
  saving = false;
  formError = '';
  toast = '';
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getTeams().subscribe({
      next: teams => {
        this.teams = teams;
        this.loadComments();
      },
      error: () => this.fail('Could not load teams. Is the backend running?')
    });
  }

  private loadComments(): void {
    this.api.getTeamComments().subscribe({
      next: comments => {
        this.comments = comments;
        this.loading = false;
        this.error = '';
      },
      error: () => this.fail('Could not load notes. Is the backend running?')
    });
  }

  private fail(message: string): void {
    this.error = message;
    this.loading = false;
  }

  // ---- derived data ----

  get members(): TeamMember[] {
    return this.teams.flatMap(t => t.members);
  }

  commentsFor(type: CommentTargetType, id: string): TeamComment[] {
    const q = this.search.trim().toLowerCase();
    return this.comments.filter(c =>
      c.targetType === type &&
      c.targetId === id &&
      (!q || c.title.toLowerCase().includes(q) || c.description.toLowerCase().includes(q)));
  }

  countFor(type: CommentTargetType, id: string): number {
    return this.comments.filter(c => c.targetType === type && c.targetId === id).length;
  }

  teamTotal(team: Team): number {
    return this.countFor('team', team.id) + team.members.reduce((n, m) => n + this.countFor('member', m.id), 0);
  }

  initials(name: string): string {
    return name.split(/\s+/).map(p => p[0]).join('').slice(0, 2).toUpperCase();
  }

  wasEdited(c: TeamComment): boolean {
    return c.updatedAt !== c.createdAt;
  }

  targetLabel(c: TeamComment): string {
    if (c.targetType === 'team') return this.teams.find(t => t.id === c.targetId)?.name ?? 'Team';
    return this.members.find(m => m.id === c.targetId)?.name ?? 'Member';
  }

  isCollapsed(id: string): boolean {
    return this.collapsedTeams.has(id);
  }

  toggleTeam(id: string): void {
    this.collapsedTeams.has(id) ? this.collapsedTeams.delete(id) : this.collapsedTeams.add(id);
  }

  // ---- popups ----

  openView(c: TeamComment): void {
    this.activeComment = c;
    this.modal = 'view';
  }

  openAdd(type?: CommentTargetType, id?: string): void {
    this.activeComment = null;
    this.formTarget = type && id ? `${type}:${id}` : this.defaultTarget();
    this.draftTitle = '';
    this.draftDescription = '';
    this.formError = '';
    this.modal = 'form';
  }

  openEdit(c: TeamComment): void {
    this.activeComment = c;
    this.formTarget = `${c.targetType}:${c.targetId}`;
    this.draftTitle = c.title;
    this.draftDescription = c.description;
    this.formError = '';
    this.modal = 'form';
  }

  openDelete(c: TeamComment): void {
    this.activeComment = c;
    this.modal = 'delete';
  }

  @HostListener('document:keydown.escape')
  closeModal(): void {
    if (this.saving) return;
    this.modal = 'none';
    this.activeComment = null;
  }

  private defaultTarget(): string {
    const first = this.teams[0];
    return first ? `team:${first.id}` : '';
  }

  // ---- CRUD ----

  save(): void {
    const title = this.draftTitle.trim();
    if (!title) {
      this.formError = 'Please add a title.';
      return;
    }
    const [targetType, targetId] = this.formTarget.split(':') as [CommentTargetType, string];
    const input = { targetType, targetId, title, description: this.draftDescription.trim() };
    const editing = this.activeComment;

    this.saving = true;
    this.formError = '';
    const request = editing
      ? this.api.updateTeamComment(editing.id, input)
      : this.api.createTeamComment(input);

    request.subscribe({
      next: () => {
        this.saving = false;
        this.modal = 'none';
        this.activeComment = null;
        this.showToast(editing ? 'Note updated' : 'Note added');
        this.loadComments();
      },
      error: () => {
        this.saving = false;
        this.formError = 'Could not save the note. Please try again.';
      }
    });
  }

  confirmDelete(): void {
    const target = this.activeComment;
    if (!target) return;
    this.saving = true;
    this.api.deleteTeamComment(target.id).subscribe({
      next: () => {
        this.saving = false;
        this.modal = 'none';
        this.activeComment = null;
        this.showToast('Note deleted');
        this.loadComments();
      },
      error: () => {
        this.saving = false;
        this.modal = 'none';
        this.showToast('Could not delete the note');
      }
    });
  }

  private showToast(message: string): void {
    this.toast = message;
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => (this.toast = ''), 2600);
  }
}

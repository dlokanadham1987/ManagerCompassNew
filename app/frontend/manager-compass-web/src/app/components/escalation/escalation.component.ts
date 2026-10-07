import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { Topic } from '../../models/topic.model';
import { TopicIconComponent } from '../../shared/topic-icon.component';

interface ContactAction {
  kind: 'mail' | 'tel' | 'link';
  href: string;
  label: string;
}

@Component({
  selector: 'app-escalation',
  standalone: true,
  imports: [CommonModule, TopicIconComponent],
  templateUrl: './escalation.component.html'
})
export class EscalationComponent implements OnInit {
  topics: Topic[] = [];
  copiedKey: string | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getTopics().subscribe(topics => (this.topics = topics));
  }

  // Contact today is a role label ("Your HRBP", "HR Shared Services (HRSC)"), not a verified
  // address — so we never fabricate a "To". If the field itself names a real email/phone/URL,
  // that becomes the direct action; otherwise we open a draft with context pre-filled and let
  // the manager add the real recipient themselves.
  contactAction(topic: Topic): ContactAction {
    const contact = topic.contact;

    const email = contact.match(/[\w.+-]+@[\w-]+\.[\w.-]+/);
    if (email) {
      return {
        kind: 'mail',
        href: `mailto:${email[0]}?subject=${encodeURIComponent(topic.label)}&body=${encodeURIComponent(topic.escalation)}`,
        label: `Email ${email[0]}`
      };
    }

    const phone = contact.match(/(\+?\d[\d\-\s().]{6,}\d)/);
    if (phone) {
      return { kind: 'tel', href: `tel:${phone[0].replace(/[^\d+]/g, '')}`, label: `Call ${phone[0]}` };
    }

    const url = contact.match(/\b((?:https?:\/\/)?(?:[a-z0-9-]+\.)+[a-z]{2,}(?:\/\S*)?)\b/i);
    if (url) {
      const href = /^https?:\/\//i.test(url[0]) ? url[0] : `https://${url[0]}`;
      return { kind: 'link', href, label: `Open ${url[0]}` };
    }

    return {
      kind: 'mail',
      href: `mailto:?subject=${encodeURIComponent(topic.label + ' — escalation')}&body=${encodeURIComponent(
        `Escalating per Manager Compass guidance:\n\n${topic.escalation}\n\nRoute to: ${topic.contact}`
      )}`,
      label: `Draft an email — add ${topic.contact}'s address`
    };
  }

  copyContact(topic: Topic): void {
    navigator.clipboard.writeText(topic.contact).then(() => {
      this.copiedKey = topic.key;
      setTimeout(() => {
        if (this.copiedKey === topic.key) this.copiedKey = null;
      }, 1500);
    });
  }
}

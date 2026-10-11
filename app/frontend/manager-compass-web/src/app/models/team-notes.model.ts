export interface TeamMember {
  id: string;
  name: string;
}

export interface Team {
  id: string;
  name: string;
  members: TeamMember[];
}

export type CommentTargetType = 'team' | 'member';

export interface TeamComment {
  id: string;
  targetType: CommentTargetType;
  targetId: string;
  title: string;
  description: string;
  createdAt: string;
  updatedAt: string;
}

export interface TeamCommentInput {
  targetType: CommentTargetType;
  targetId: string;
  title: string;
  description: string;
}

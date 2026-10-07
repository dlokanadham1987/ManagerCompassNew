using ManagerCompass.Api.Models;

namespace ManagerCompass.Api.Services;

/// <summary>
/// Guidance text is grounded in real RealPage HR documents (hardcoded here — see each Topic's
/// Sources). The People Snapshot is read LIVE from the sponsor's 10_Sample HR Dataset export at
/// startup via SampleHrDatasetReader; if that folder isn't available on this machine, it falls
/// back to the last-known real numbers so the app still runs for a teammate without local access
/// to the dataset. See 03_Final_Submission/README.md for details.
/// </summary>
public class MockGuidanceDataService : IGuidanceDataService
{
    private readonly Snapshot _snapshot;
    private readonly ILogger<MockGuidanceDataService> _logger;

    public MockGuidanceDataService(IConfiguration configuration, ILogger<MockGuidanceDataService> logger)
    {
        _logger = logger;
        var datasetPath = SampleHrDatasetReader.ResolveConfiguredPath(configuration);
        try
        {
            if (string.IsNullOrWhiteSpace(datasetPath) || !Directory.Exists(datasetPath))
                throw new DirectoryNotFoundException($"Sample HR dataset folder not found: {datasetPath}");

            _snapshot = new SampleHrDatasetReader().Read(datasetPath);
            _logger.LogInformation("People Snapshot loaded LIVE from {Path}", datasetPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read live sample HR dataset from {Path} — falling back to last-known real numbers", datasetPath);
            _snapshot = FallbackSnapshot();
        }
        _snapshot.LoadedAtUtc = DateTime.UtcNow;
    }

    private static readonly List<string> GuardrailPhrases = new()
    {
        "salary", "pay stub", "how much does", "ssn", "social security", "medical condition",
        "pregnan", "disability status", "visa status", "immigration status", "fire him", "fire her",
        "terminate", "lawsuit", " sue ", "divorce", "home address", "rating for", "review score for",
        "pay of", "compensation for", "what does he make", "what does she make", "w-2", "w2 for"
    };

    // Answers, checklists, and escalation text below are grounded in real RealPage HR documents
    // found in the sponsor's OneDrive_1_9-16-2026 folder (see Sources on each topic and
    // 03_Final_Submission/README.md). Where a document didn't cover something, that gap is
    // stated honestly rather than invented (see Compensation and Team health).
    private readonly List<Topic> _topics = new()
    {
        new Topic
        {
            Key = "recruiting", Label = "Recruiting & hiring", Blurb = "Requisitions, interviews, roles",
            Color = "#2C7A78", Contact = "HR Shared Services (HRSC)",
            Prompt = "How do I open a requisition?",
            Answer = "To open a requisition, email your complete request to HR Shared Services (HRSC) — including the correct Job Code Template and any downgrade/upgrade/job code update details — and HRSC will create it in RecruitIQ within 24 hours and route it for approval. If you create the requisition yourself in RecruitIQ first, HRSC still needs to validate it, route it for approval, and add the required collaborators.",
            Sources = new() { "Requisition Intake.pdf" },
            Checklist = new()
            {
                "Email HRSC with your complete requisition request, including any downgrade, upgrade, or job code update details",
                "Confirm the correct Job Code Template, since it drives pay grade, position, location, and salary alignment",
                "Provide the job description so it can be inserted into the requisition",
                "If you create the requisition yourself in RecruitIQ (Create > Requisition), tell HRSC so they can validate it and route it for approval"
            },
            Escalation = "Hand off to HR/Finance rather than acting alone for IT requisitions, which need HR, Finance, and IT leadership sign-off before HRSC will create them, and for Professional Services or Support and RPCC requisitions, where the forecast date must be confirmed with the relevant finance/operations lead before the requisition is created and routed.",
            Keywords = new() { "requisition", "req", "hire", "hiring", "interview", "recruit", "job posting", "candidate", "headcount request" },
            ExamplePrompts = new()
            {
                "What do I need to send HRSC to open a requisition?",
                "Can I create the requisition myself in RecruitIQ?",
                "What's different about an IT or Professional Services requisition?"
            }
        },
        new Topic
        {
            Key = "compensation", Label = "Compensation & merit", Blurb = "Merit planning, pay prep",
            Color = "#7C5C99", Contact = "HR / Compensation",
            Prompt = "How do I prepare for merit planning?",
            Answer = "The available documents don't cover merit or comp planning directly — they cover RealPage's Talent Review process in UKG and the official 5-point performance rating scale. What they do establish is the evidence base any merit/comp process draws on: each employee's Current Performance Level, potential/promotability rating, and documented strengths and development notes in UKG.",
            Sources = new() { "Talent Reviews and UKG Playbook 2025.pdf", "RealPage Evaluation Ratings Definition.pdf" },
            Checklist = new()
            {
                "Confirm each direct report's most recently completed performance review rating is finalized",
                "In UKG (My Team > Talent Management > Talent Dashboard), set/update each employee's Current Performance Level and add supporting notes",
                "Document 2-3 Strengths and 2-3 Development Opportunities per employee so any differentiation is backed by specific evidence",
                "Review each employee's 9-box placement (performance x potential) ahead of calibration"
            },
            Escalation = "These documents contain no guidance on merit budgets, increase percentages, or pay-decision rules — involve HR/Compensation for anything beyond documenting performance, potential, and ratings in UKG.",
            Keywords = new() { "merit", "compensation", "comp", "raise", "increase", "pay planning", "calibration", "bonus" },
            ExamplePrompts = new()
            {
                "What should I document before calibration?",
                "Where do I set an employee's Current Performance Level in UKG?",
                "What does a 9-box placement need before merit season?"
            }
        },
        new Topic
        {
            Key = "engagement", Label = "Engagement", Blurb = "Scores, listening, follow-up",
            Color = "#E4693F", Contact = "Your HRBP",
            Prompt = "Our team's engagement score dropped — what should I do?",
            Answer = "An engagement score drop is best addressed through an honest, two-way team conversation rather than a top-down fix — employees who have a two-way conversation about a change reach far higher understanding than those who are just told, and are far less likely to feel angry when included in the dialogue. Prepare by understanding your own reaction and the team's situation first, then hold a discussion where you genuinely listen and work with them to rebuild trust.",
            Sources = new() { "Change Management - Manager Toolkit V2.pdf", "Team Discussion Framework- Leader's Guide 1.pdf" },
            Checklist = new()
            {
                "Before meeting with the team, process your own reaction and clarify what's driving the drop and how it affects the team",
                "Hold a dedicated team discussion: acknowledge the concern, set ground rules for a safe space, make clear input will be used constructively",
                "Ask open-ended questions and actively listen — \"What could we be doing better?\", \"How are you feeling?\"",
                "Work with the team to identify what they can influence, agree on next steps together, and close with a summary and a follow-up plan"
            },
            Escalation = "Reach out to your HRBP if you want a member of the HR team to co-facilitate the discussion, or for specific guidance on your team's situation.",
            Keywords = new() { "engagement", "engagement score", "survey", "feedback", "listening session", "morale" },
            ExamplePrompts = new()
            {
                "How do I run a listening session after a low score?",
                "How do I turn engagement feedback into a follow-up plan?",
                "How do I prepare for a team discussion about morale?"
            }
        },
        new Topic
        {
            Key = "retention", Label = "Turnover & retention", Blurb = "Stay conversations, signals",
            Color = "#1B6E6B", Contact = "Your HRBP",
            Prompt = "One of my top performers seems like a retention risk.",
            Answer = "Treat this as an open, two-way conversation rather than a fix to hand down — ask open-ended questions, actively listen, and let the employee surface what's really going on before you react with a plan.",
            Sources = new() { "Team Discussion Framework- Leader's Guide 1.pdf", "Offboarding_Process_in_UKG_Revised.pdf" },
            Checklist = new()
            {
                "Prepare first: process your own reaction and think through what you understand (and don't) about what's driving their disengagement",
                "Open with empathy and an invitation to share — e.g. \"How are you feeling about things right now?\" — and be an active listener",
                "Ask what specific challenges they're facing and what resources or support would help, then discuss solutions together",
                "Close with a clear recap, express appreciation for their openness, and schedule a genuine follow-up conversation"
            },
            Escalation = "Loop in your HRBP if you want support having the conversation itself (HR co-facilitation is available), or as soon as the employee actually initiates a resignation — offboarding requires you to coordinate with your HRBP on separation details before approving that request.",
            Keywords = new() { "retention", "flight risk", "resign", "quit", "turnover", "stay conversation", "leaving" },
            ExamplePrompts = new()
            {
                "How do I start a stay conversation?",
                "What do I do if an employee is about to resign?",
                "How do I prepare before talking to a flight-risk employee?"
            }
        },
        new Topic
        {
            Key = "performance", Label = "Performance & career", Blurb = "Goals, development, documentation",
            Color = "#B85C72", Contact = "Your HRBP",
            Prompt = "How should I run a development conversation this week?",
            Answer = "Treat this as a development conversation, not a pay conversation: come with concrete examples of wins and growth areas, use structured feedback (STAR for praise, STAR/AR for coaching), and leave with a documented, SMART development goal that has an owner, milestone, and date.",
            Sources = new() { "Manager User Guide - Performance Conversation.pdf", "Making Performance Goals SMART (DDI).pdf", "Goal-Setting (FAQs).pdf" },
            Checklist = new()
            {
                "Schedule 45-60 minutes, share the agenda in advance, and ask for the teammate's self-evaluation and career aspirations beforehand",
                "Pull concrete examples from your evaluation/calibration notes and identify key wins and growth areas before the conversation",
                "During the conversation, open with recognition citing specific outcomes, focus on behaviors and results, and use STAR for praise or STAR/AR for coaching",
                "Afterward, document the development goal using the SMART formula, summarize agreements, and schedule a follow-up touchpoint"
            },
            Escalation = "Involve your HR Business Partner when a situation falls outside standard goal management (e.g. an employee transferring departments with existing goals) or when you need broader support with the goal-setting or performance process.",
            Keywords = new() { "performance", "career", "development", "goal", "promotion", "review", "feedback conversation" },
            ExamplePrompts = new()
            {
                "How do I document a SMART development goal?",
                "What feedback framework should I use for a coaching conversation?",
                "How should I prepare for a career conversation?"
            }
        },
        new Topic
        {
            Key = "policy", Label = "Policy", Blurb = "Current guidance lookup",
            Color = "#3E6E93", Contact = "Your HRBP",
            Prompt = "Where do I find the current policy on this?",
            Answer = "The Employee Handbook itself says it's general guidance, not comprehensive, and that the latest version is always on the RealPage Global Portal — pull the current copy rather than relying on a saved or printed version. For deeper detail, the handbook points to specific systems (UKG for Flex First, Tuition Assistance, and Travel & Expense; the Benefits SharePoint site for benefits) or standalone policies with their own version history, like the Internal Mobility Policy.",
            Sources = new() { "Employee-Handbook--1.12.26-v2.pdf", "RealPage - Internal Mobility Policy 20250729.pdf" },
            Checklist = new()
            {
                "Pull the current Employee Handbook from the RealPage Global Portal rather than a saved or printed copy",
                "For topics that reference a specific system (Flex First, Tuition Assistance, Travel & Expense in UKG; benefits detail on the Benefits SharePoint site), go to that system for the current detail",
                "For standalone policies like the Internal Mobility Policy, check the \"Document Control\" section for the author, date, and change description to confirm the most recent revision",
                "If the handbook's general description doesn't clearly answer your situation, don't guess — the handbook itself says to direct specific questions to HR"
            },
            Escalation = "Involve your HRBP whenever the handbook's language is general, a policy's applicability to your situation is unclear, or you can't confirm you're looking at the current version.",
            Keywords = new() { "policy", "handbook", "pto policy", "leave policy", "dress code", "remote work policy" },
            ExamplePrompts = new()
            {
                "How do I confirm I'm looking at the current handbook version?",
                "Where do I check Flex First, Tuition Assistance, or Travel & Expense?",
                "Where do I find the Internal Mobility Policy's latest revision?"
            }
        },
        new Topic
        {
            Key = "payroll", Label = "Payroll & benefits", Blurb = "Routing, not resolving",
            Color = "#C1633C", Contact = "System Admin (UKG) / Unum (leave)",
            Prompt = "An employee has a payroll or benefits question — what do I do?",
            Answer = "For payroll items like direct deposit, the employee should use self-service in UKG Pro (Menu > Myself > Pay > Direct Deposit); if the system won't let them, that's usually a configuration issue for the system administrator, not something a manager can fix. For benefits or leave questions, route the employee to the specific plan administrator named in the Benefits Summary rather than explaining plan details yourself.",
            Sources = new() { "Add or Change Direct Deposit Accounts.pdf", "2026 Benefits Summary.pdf", "LOA Information.pdf" },
            Checklist = new()
            {
                "Direct deposit: point the employee to Menu > Myself > Pay > Direct Deposit in UKG Pro to add, change, or archive an account themselves",
                "If the employee can't edit their direct deposit page, escalate to the system administrator — don't attempt to enter or view their banking details yourself",
                "FMLA, Parental Leave, or Short Term Disability: tell the employee to contact Unum directly (portal.unum.com or 1-866-868-6737) as soon as the leave officially starts",
                "General benefits questions: direct the employee to the relevant named administrator in the Benefits Summary, or point them to request the full RealPage Benefits Guide"
            },
            Escalation = "No single centralized \"Payroll/Benefits support\" team is named — escalate UKG direct-deposit access/configuration issues to the system administrator, and escalate any FMLA/STD/leave activation issue straight to Unum, since the manager has no role in processing either.",
            Keywords = new() { "payroll", "benefits", "paycheck", "enrollment", "w2", "direct deposit", "insurance" },
            ExamplePrompts = new()
            {
                "An employee can't update their direct deposit — what do I do?",
                "An employee needs to start FMLA or short-term disability — who do I contact?",
                "Where do I send a general benefits enrollment question?"
            }
        },
        new Topic
        {
            Key = "teamhealth", Label = "Team health", Blurb = "Signals & org snapshot",
            Color = "#6E8C52", Contact = "Your HRBP",
            Prompt = "Walk me through my team's current signals.",
            Answer = "The team-building library isn't a diagnostics or signals framework — it's 20 facilitated activities with no metrics or health scores. Use it as your response toolkit: when you notice friction or disconnection, pick an activity matched to what you're seeing, and let its built-in debrief conversation — not a number — be how you actually learn what's going on. Pair it with the real People Snapshot numbers for the org-level signal.",
            Sources = new() { "Team Building and Icebreaker Activities V2.1.pdf" },
            Checklist = new()
            {
                "Diagnose first: choose a Team Building activity if the concern is cohesion/trust, or an Icebreaker if the concern is people not knowing each other well",
                "Check the face-to-face-vs-virtual breakdown before scheduling — several activities are F2F only",
                "Tell the team why you're running it, and frame it as collaboration, not competition",
                "Afterward, use the activity's own debrief questions as a real conversation with the team, not just a wrap-up exercise"
            },
            Escalation = "Reach out to your HRBP for questions or support in planning or facilitating these activities.",
            Keywords = new() { "team health", "headcount", "attrition", "org chart", "span of control", "snapshot" },
            ExamplePrompts = new()
            {
                "My team seems disconnected — what activity should I run?",
                "How do I debrief after a team-building activity?",
                "Should I run a virtual or in-person activity?"
            }
        },
        new Topic
        {
            Key = "onboarding", Label = "New manager onboarding", Blurb = "30 / 60 / 90 day plan",
            Color = "#2E8C9C", Contact = "HR / Your Direct Leader",
            Prompt = "I'm a new manager — what are my first 90 days?",
            Answer = "RealPage's New Leader Toolkit lays out an explicit 30-60-90 day path: Week 1 is meeting your team, meeting with HR or your direct leader, and confirming systems access; Weeks 2-4 add compliance training, orientation, and building a 90-day quick-win plan; by Day 30 you and your team must have goals entered in UKG.",
            Sources = new() { "New Leader Toolkit V5.1.pdf" },
            Checklist = new()
            {
                "Day 1: Meet with HR or your direct leader to cover onboarding details, meet your team, and confirm you have systems access for your scope of work",
                "Days 2-3: Complete all assigned Legal Compliance Training and HR Compliance videos in RealPage University before New Hire Orientation",
                "Week 4: Develop your 90-day quick-win plan, and ensure you and your team have goals set in UKG (3-5 performance goals plus at least 1 developmental goal)",
                "60-90 Days: Review your 90-day quick-win plan and review goal progress, identifying areas of improvement"
            },
            Escalation = "Meet with HR or your direct leader on Day 1 for onboarding details, and reach out to your HRBP for any other HR policy or procedure not covered in the toolkit.",
            Keywords = new() { "new manager", "onboarding", "first 90 days", "30 60 90", "just became a manager" },
            ExamplePrompts = new()
            {
                "What should I do on Day 1 as a new manager?",
                "What goals should be set in UKG by day 30?",
                "What does my 90-day quick-win plan need to cover?"
            }
        }
    };

    public IReadOnlyList<Topic> GetTopics() => _topics;

    public Topic? GetTopic(string key) =>
        _topics.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));

    public Snapshot GetSnapshot() => _snapshot;

    // Used only if the live dataset folder isn't reachable on this machine (see constructor).
    // These are the same real numbers the live reader computed on 2026-09-16 — a frozen copy,
    // not invented data, so the app still runs for a teammate without local access to the dataset.
    private static Snapshot FallbackSnapshot() => new()
    {
        ScopeLabel = "Sample HR Dataset — company-wide",
        SourceNote = "Live read of the sponsor's de-identified 10_Sample HR Dataset export was unavailable on this machine — showing the last-known real counts instead. Only single-column totals are ever shown; the dataset's row-level combinations were intentionally shuffled for privacy and are not valid to reconstruct.",
        Kpis = new()
        {
            new SnapshotKpi { Label = "Active headcount", Value = "8,306", SubText = "active_fte.xlsx, sample export", Color = "#2C7A78" },
            new SnapshotKpi { Label = "Open requisitions", Value = "449", SubText = "298 approved · 88 pending · 63 hold", Color = "#2E8C9C" },
            new SnapshotKpi { Label = "Exits, sample", Value = "1,329", SubText = "~13.8% of headcount+exits (estimate)", Color = "#6E8C52" },
            new SnapshotKpi { Label = "Contingent workforce", Value = "761", SubText = "Contractors, interns & FaCT consultants", Color = "#7C5C99" },
            new SnapshotKpi { Label = "Engagement", Value = "—", SubText = "Illustrative only — no source column in sample dataset", Color = "#E4693F" },
            new SnapshotKpi { Label = "Span of control", Value = "7.6", SubText = "1,164 managers · active_fte.xlsx", Color = "#1B6E6B" },
        },
        LevelLabels = new() { "L1", "L2", "L3", "L4", "L5", "L6", "L7", "L8", "L9" },
        LevelCounts = new() { 1, 12, 74, 346, 982, 2725, 2744, 1297, 125 },
        EngagementTrend = new() { 75, 71, 69, 72 },
        AttritionByReason = new()
        {
            new AttritionReason { Label = "Career opportunity", Percent = 29.3, Color = "#7C5C99" },
            new AttritionReason { Label = "Reduction in force / realign", Percent = 15.7, Color = "#B85C72" },
            new AttritionReason { Label = "Performance / behavior / attend.", Percent = 14.1, Color = "#2E8C9C" },
            new AttritionReason { Label = "Personal / family", Percent = 8.3, Color = "#C1633C" },
        },
        AttritionByType = new()
        {
            new AttritionReason { Label = "Non-Regrettable", Percent = 52.4, Color = "#2C7A78" },
            new AttritionReason { Label = "Regrettable", Percent = 40.3, Color = "#C8402F" },
        },
        EarlyTenureExitPct = 18.4,
        HeadcountByRegion = new()
        {
            new AttritionReason { Label = "USA", Percent = 38.0, Color = "#2C7A78" },
            new AttritionReason { Label = "IND", Percent = 31.7, Color = "#2E8C9C" },
            new AttritionReason { Label = "PHL", Percent = 29.7, Color = "#E4693F" },
            new AttritionReason { Label = "ISR", Percent = 0.2, Color = "#7C5C99" },
        },
        RequisitionStatus = new()
        {
            new RequisitionStatusRow { Status = "Approved", Count = 298, Color = "#2C7A78" },
            new RequisitionStatusRow { Status = "Pending approval", Count = 88, Color = "#E4693F" },
            new RequisitionStatusRow { Status = "Hold", Count = 63, Color = "#C1633C" },
        }
    };

    public LeadershipCard GetLeadershipCard()
    {
        var risks = new List<LeadershipItem>
        {
            new() { Text = "Engagement dipped 3 pts QoQ (72, prior 75) — workload named in 2 of 3 listening sessions.", Source = "HR Engagement Playbook · listening-session notes (synthetic)", Color = "#E4693F" },
            new() { Text = "One tenured leasing consultant flagged as a retention risk after a schedule-change signal.", Source = "Stay Conversation Guide · manager notes (synthetic)", Color = "#1B6E6B" },
            new() { Text = "Assistant Community Manager requisition has sat in intake for 12 days without posting.", Source = "Requisition system (synthetic)", Color = "#2C7A78" },
        };

        // The pulse badge is computed from this same period's data, not a fixed label — it moves
        // if the risk count or engagement score changes next period.
        const int engagementScore = 72;
        string pulseLabel, pulseColor, pulseNote;
        if (risks.Count >= 3 || engagementScore < 70)
        {
            pulseLabel = "Needs attention";
            pulseColor = "#C97F1E";
            pulseNote = $"{risks.Count} open risks this period, including a retention signal and a stalled requisition";
        }
        else if (risks.Count == 2 || engagementScore < 75)
        {
            pulseLabel = "Watch";
            pulseColor = "#2E8C9C";
            pulseNote = $"{risks.Count} items flagged this period — worth a check-in before next review";
        }
        else
        {
            pulseLabel = "On track";
            pulseColor = "#2C7A78";
            pulseNote = "No material risks flagged this period";
        }

        return new LeadershipCard
        {
            TeamName = "Riverbend Community Management Team",
            Manager = "Lokanadham Dasamukha",
            ReviewedWith = "Regional Director",
            Period = "Q1 FY27",
            CardNumber = "MC-07 · Card No. 001",
            PulseLabel = pulseLabel,
            PulseColor = pulseColor,
            PulseNote = pulseNote,
            Stats = new()
            {
                new SnapshotKpi { Label = "Headcount", Value = "14", SubText = "Illustrative example team", Color = "#2C7A78" },
                new SnapshotKpi { Label = "Open roles", Value = "2", SubText = "Illustrative example team", Color = "#2E8C9C" },
                new SnapshotKpi { Label = "Attrition, 12mo", Value = "9%", SubText = "Illustrative example team", Color = "#6E8C52" },
                new SnapshotKpi { Label = "Engagement", Value = engagementScore.ToString(), SubText = "Illustrative example team", Color = "#E4693F" },
                new SnapshotKpi { Label = "Org layers to VP", Value = "3", SubText = "Illustrative example team", Color = "#7C5C99" },
            },
            RisksFlagged = risks,
            ActionsTaken = new()
            {
                new LeadershipItem { Text = "Ran 3 listening sessions and shared a \"you said / we did\" note with the team.", Color = "#6E8C52" },
                new LeadershipItem { Text = "Held a stay conversation with the flagged consultant — no resignation signal confirmed at this time.", Color = "#6E8C52" },
                new LeadershipItem { Text = "Escalated the stalled requisition to the recruiting partner for intake review.", Color = "#6E8C52" },
            },
            NextSteps = new()
            {
                new LeadershipItem { Text = "Re-check the engagement pulse once the \"you said / we did\" actions have landed.", Color = "#7C5C99" },
                new LeadershipItem { Text = "Confirm the open requisition posts within 5 business days.", Color = "#7C5C99" },
                new LeadershipItem { Text = "Complete merit-prep documentation ahead of the calibration window.", Color = "#7C5C99" },
            },
            EvidenceNote = "Evidence: Engagement Playbook, Stay Conversation Guide, Requisition Intake Guide, Measure What Matters (all synthetic placeholders)",
            HumanReviewNote = "Human review: HRBP for retention & comp items before any action is finalized"
        };
    }

    public AdoptionMetrics GetAdoptionMetrics() => new()
    {
        ActiveManagers = 9,
        EligibleManagers = 14,
        QuestionsAnsweredThisWeek = 47,
        ChecklistCompletionRate = 0.68,
        EscalationsRoutedThisWeek = 6,
        MedianSecondsPerQuestion = 38,
        WeeklyActiveTrend = new() { 3, 5, 7, 9 },
        TopTopics = new()
        {
            new TopicUsage { Key = "engagement", Label = "Engagement", Color = "#E4693F", Count = 14 },
            new TopicUsage { Key = "recruiting", Label = "Recruiting & hiring", Color = "#2C7A78", Count = 11 },
            new TopicUsage { Key = "retention", Label = "Turnover & retention", Color = "#1B6E6B", Count = 8 },
            new TopicUsage { Key = "compensation", Label = "Compensation & merit", Color = "#7C5C99", Count = 6 },
            new TopicUsage { Key = "onboarding", Label = "New manager onboarding", Color = "#2E8C9C", Count = 5 },
        },
        // All 9 topics — counts sum to QuestionsAnsweredThisWeek (47), escalation counts sum to
        // EscalationsRoutedThisWeek (6), and completion rates weighted-average to ChecklistCompletionRate
        // (68%) across the topics that had any questions this week. Zero-question topics show null
        // rates rather than a misleading 0%.
        TopicBreakdown = new()
        {
            new TopicAdoptionStat { Key = "engagement", Label = "Engagement", Color = "#E4693F", Count = 14, EscalationRatePct = 7.1, ChecklistCompletionRatePct = 75, MedianSeconds = 32 },
            new TopicAdoptionStat { Key = "recruiting", Label = "Recruiting & hiring", Color = "#2C7A78", Count = 11, EscalationRatePct = 9.1, ChecklistCompletionRatePct = 70, MedianSeconds = 45 },
            new TopicAdoptionStat { Key = "retention", Label = "Turnover & retention", Color = "#1B6E6B", Count = 8, EscalationRatePct = 25, ChecklistCompletionRatePct = 60, MedianSeconds = 40 },
            new TopicAdoptionStat { Key = "compensation", Label = "Compensation & merit", Color = "#7C5C99", Count = 6, EscalationRatePct = 33.3, ChecklistCompletionRatePct = 55, MedianSeconds = 55 },
            new TopicAdoptionStat { Key = "onboarding", Label = "New manager onboarding", Color = "#2E8C9C", Count = 5, EscalationRatePct = 0, ChecklistCompletionRatePct = 80, MedianSeconds = 30 },
            new TopicAdoptionStat { Key = "performance", Label = "Performance & career", Color = "#B85C72", Count = 2, EscalationRatePct = 0, ChecklistCompletionRatePct = 65, MedianSeconds = 42 },
            new TopicAdoptionStat { Key = "policy", Label = "Policy", Color = "#8A9096", Count = 1, EscalationRatePct = 0, ChecklistCompletionRatePct = 60, MedianSeconds = 25 },
            new TopicAdoptionStat { Key = "payroll", Label = "Payroll & benefits", Color = "#C1633C", Count = 0, EscalationRatePct = null, ChecklistCompletionRatePct = null, MedianSeconds = null },
            new TopicAdoptionStat { Key = "teamhealth", Label = "Team health", Color = "#6E8C52", Count = 0, EscalationRatePct = null, ChecklistCompletionRatePct = null, MedianSeconds = null },
        },
        ProductionMeasurementPlan = new()
        {
            "Log each question's topic, escalation outcome, and checklist completion as an event, not a survey",
            "Report weekly active managers against the eligible manager population from Org Insights, not raw question volume alone",
            "Track escalation-to-resolution time with HR as the leading adoption-quality signal, not just usage count",
            "Review adoption alongside engagement-score movement to see if usage correlates with the outcomes it's meant to drive"
        }
    };

    public AskResponse Ask(AskRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.TopicKey))
        {
            var forced = GetTopic(request.TopicKey);
            if (forced != null) return new AskResponse { Type = "topic", Topic = forced };
        }

        var text = " " + (request.Text ?? "").ToLowerInvariant() + " ";

        if (GuardrailPhrases.Any(p => text.Contains(p)))
        {
            return new AskResponse
            {
                Type = "guardrail",
                Message = "I can't help with details about a specific person's pay, performance rating, medical situation, or employment status — that needs a human decision with HR, not an assistant. Contact your HRBP directly for this. Manager Compass only surfaces approved, non-sensitive guidance and aggregated team data — never individual records."
            };
        }

        Topic? best = null;
        int bestScore = 0;
        foreach (var t in _topics)
        {
            int score = t.Keywords.Count(k => text.Contains(k));
            if (score > bestScore) { bestScore = score; best = t; }
        }

        if (best != null) return new AskResponse { Type = "topic", Topic = best };

        return new AskResponse
        {
            Type = "clarify",
            Message = "I want to point you to the right guidance, but I need a bit more detail. Is this about recruiting, compensation, engagement, retention, performance, policy, payroll/benefits, team health, or new-manager onboarding?"
        };
    }
}

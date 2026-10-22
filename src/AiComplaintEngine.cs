using System;
using System.Collections.Generic;
using System.Linq;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public class AiComplaintSession
{
    public string Category { get; set; } = "General";
    public string Priority { get; set; } = "Medium";
    public string IdentifiedProblem { get; set; } = "";
    public string ClarificationDetails { get; set; } = "";
    public int ConversationStep { get; set; } = 0;
    public bool IsReadyToSubmit { get; set; } = false;
    public List<AiChatMessage> Messages { get; set; } = new();

    public string FormattedTitle { get; set; } = "";
    public string FormattedDescription { get; set; } = "";
}

public class AiComplaintEngine
{
    public static AiComplaintSession StartNewSession()
    {
        var session = new AiComplaintSession();
        string user = Environment.UserName;
        string host = Environment.MachineName;

        var greeting = new AiChatMessage
        {
            Sender = "AI",
            Text = $"👋 Hello **{user}**! I am your **SVLL IT Diagnostic AI Assistant** on `{host}`.\n\n" +
                   "I can help resolve your workstation issue or ask a few quick clarifying questions to prepare a prioritized technical ticket for IT Support.\n\n" +
                   "What problem are you experiencing right now?",
            QuickReplies = new List<string>
            {
                "🖨️ Thermal Printer / Zebra Skipping",
                "🌐 WMS or Internet Disconnected",
                "⚡ Workstation Freezing or Slow",
                "🔒 USB Flash Drive Blocked by DLP",
                "🩺 Blue Screen or Windows Error"
            }
        };

        session.Messages.Add(greeting);
        return session;
    }

    public static AiChatMessage ProcessUserMessage(AiComplaintSession session, string userText)
    {
        // Record user message
        var userMsg = new AiChatMessage
        {
            Sender = "User",
            Text = userText,
            Timestamp = DateTime.Now
        };
        session.Messages.Add(userMsg);

        string lower = userText.ToLowerInvariant();

        // Check if user confirmed ticket submission
        if (session.IsReadyToSubmit && (lower.Contains("submit") || lower.Contains("confirm") || lower.Contains("file ticket") || lower.Contains("yes")))
        {
            var ticket = ComplaintTicketService.Instance.CreateTicket(
                session.FormattedTitle,
                session.FormattedDescription,
                session.Category,
                session.Priority,
                "Ext 101",
                FleetRegistryService.Instance.LocalDevice.BranchLocation,
                BuildTranscriptString(session),
                filedViaAi: true
            );

            session.ConversationStep = 99;
            return new AiChatMessage
            {
                Sender = "AI",
                Text = $"🎉 **Complaint Ticket Successfully Registered!**\n\n" +
                       $"• **Ticket ID**: `{ticket.TicketId}`\n" +
                       $"• **Category**: {ticket.Category}\n" +
                       $"• **Assigned To**: {ticket.AssignedTechnician}\n" +
                       $"• **Status**: Open (High Priority)\n\n" +
                       $"Your system diagnostics (IP: `{ticket.IpAddress}`, AnyDesk: `{ticket.AnyDeskId}`) have been forwarded directly to the IT Admin Console. You can track this ticket anytime under 'Track My Submitted Tickets'.",
                QuickReplies = new List<string> { "Start Another Complaint", "Check My Tickets" }
            };
        }

        // Conversation Step Logic
        if (session.ConversationStep == 0)
        {
            // Domain identification
            if (lower.Contains("print") || lower.Contains("zebra") || lower.Contains("tsc") || lower.Contains("label") || lower.Contains("barcode") || lower.Contains("spool"))
            {
                session.Category = "ThermalPrinter";
                session.Priority = "High";
                session.IdentifiedProblem = "Zebra / Thermal Printer Malfunction";
                session.ConversationStep = 1;

                return new AiChatMessage
                {
                    Sender = "AI",
                    Text = "🔍 I've identified a **Logistics Thermal / Barcode Printer** issue.\n\n" +
                           "To help IT diagnose the root cause immediately:\n" +
                           "1. Is the printer status LED **Solid Green**, **Blinking Red/Amber**, or completely off?\n" +
                           "2. Is the print job stuck in the Windows spooler, or are blank labels feeding out?",
                    QuickReplies = new List<string>
                    {
                        "🔴 Blinking Red / Paper Jam Light",
                        "📄 Ejecting Blank / Misaligned Labels",
                        "⏳ Stuck in Windows Print Queue",
                        "⚡ Printer Not Powering On"
                    },
                    ActionCommand = "FLUSH_SPOOLER",
                    ActionLabel = "Restart Print Spooler Now"
                };
            }
            else if (lower.Contains("net") || lower.Contains("internet") || lower.Contains("wms") || lower.Contains("erp") || lower.Contains("wifi") || lower.Contains("dns") || lower.Contains("disconnect"))
            {
                session.Category = "Network";
                session.Priority = "High";
                session.IdentifiedProblem = "Network & Logistics ERP Connectivity Failure";
                session.ConversationStep = 1;

                return new AiChatMessage
                {
                    Sender = "AI",
                    Text = "🌐 I've identified a **Network / Logistics ERP Connectivity** issue.\n\n" +
                           "Let's pinpoint where the connection is dropping:\n" +
                           "1. Can you open general websites (e.g. google.com), or is all internet down?\n" +
                           "2. Does the ERP display 'Connection Timed Out' or 'Host Unreachable'?",
                    QuickReplies = new List<string>
                    {
                        "🏢 Only WMS / Logistics Portal is Down",
                        "🚫 Complete Internet Outage",
                        "📶 Wi-Fi Signal Dropping Repeatedly",
                        "🔒 SSL / Certificate Error"
                    },
                    ActionCommand = "FLUSH_DNS",
                    ActionLabel = "Flush DNS & Reset IP Cache"
                };
            }
            else if (lower.Contains("slow") || lower.Contains("freeze") || lower.Contains("hang") || lower.Contains("ram") || lower.Contains("cpu") || lower.Contains("lag"))
            {
                session.Category = "Hardware";
                session.Priority = "Medium";
                session.IdentifiedProblem = "System Resource Saturation & Workstation Freeze";
                session.ConversationStep = 1;

                return new AiChatMessage
                {
                    Sender = "AI",
                    Text = "⚡ I've identified a **System Slowness / Unresponsive Workstation** issue.\n\n" +
                           "To triage hardware vs background bloat:\n" +
                           "1. Does the freeze happen when opening specific software (e.g. Chrome, Excel), or continuously?\n" +
                           "2. How long has this workstation been powered on without a reboot?",
                    QuickReplies = new List<string>
                    {
                        "💻 Freeze when opening Chrome / ERP",
                        "🔥 100% Constant Disk or CPU Load",
                        "⏳ Boot and Login Takes 10+ Minutes",
                        "🖱️ Mouse Cursor Stutters / Freezes"
                    },
                    ActionCommand = "TURBO_BOOST",
                    ActionLabel = "Trigger 1-Click Turbo Boost Reclaim"
                };
            }
            else if (lower.Contains("usb") || lower.Contains("pen drive") || lower.Contains("dlp") || lower.Contains("flash drive") || lower.Contains("blocked"))
            {
                session.Category = "UsbDlp";
                session.Priority = "Medium";
                session.IdentifiedProblem = "USB Storage DLP Policy Block";
                session.ConversationStep = 1;

                return new AiChatMessage
                {
                    Sender = "AI",
                    Text = "🔒 I've identified a **USB Storage / DLP Endpoint Security** request.\n\n" +
                           "Under corporate policy, USB flash drives are restricted to prevent data leakage.\n" +
                           "1. Do you need **Read Access** (viewing files on USB) or **Write Access** (copying files to USB)?\n" +
                           "2. What is the business purpose (e.g., driver manifests, customs bill, transport invoice)?",
                    QuickReplies = new List<string>
                    {
                        "📄 Need to copy Shipping Manifests TO USB",
                        "👁️ Need to READ documents from vendor USB",
                        "🚫 USB Drive Not Recognized in My PC"
                    }
                };
            }
            else
            {
                session.Category = "General";
                session.Priority = "Medium";
                session.IdentifiedProblem = userText;
                session.ConversationStep = 1;

                return new AiChatMessage
                {
                    Sender = "AI",
                    Text = $"Understood. You're reporting: *\"{userText}\"*.\n\n" +
                           "How critically is this impacting your warehouse operations today?",
                    QuickReplies = new List<string>
                    {
                        "🚨 Critical - Dispatch / Warehouse Halted",
                        "🟠 High - Shipments Delayed",
                        "🟡 Medium - Workaround Available",
                        "🔵 Low - General Maintenance Request"
                    }
                };
            }
        }
        else if (session.ConversationStep == 1)
        {
            // Clarification details received
            session.ClarificationDetails = userText;
            session.ConversationStep = 2;

            if (lower.Contains("critical") || lower.Contains("halt")) session.Priority = "Critical";
            else if (lower.Contains("high") || lower.Contains("delay")) session.Priority = "High";
            else if (lower.Contains("low")) session.Priority = "Low";

            // Formulate Ticket
            string host = Environment.MachineName;
            string ip = FleetRegistryService.Instance.LocalDevice.IpAddress;

            session.FormattedTitle = $"{session.IdentifiedProblem} on {host}";
            session.FormattedDescription = 
                $"User Statement: {session.Messages.FirstOrDefault(m => m.Sender == "User")?.Text}\n" +
                $"Diagnostic Clarification: {session.ClarificationDetails}\n" +
                $"Triage Category: {session.Category}\n" +
                $"Assessed Severity: {session.Priority}\n" +
                $"Workstation Host: {host} (IP: {ip})\n" +
                $"AnyDesk ID: {FleetRegistryService.Instance.LocalDevice.AnyDeskId}";

            session.IsReadyToSubmit = true;

            return new AiChatMessage
            {
                Sender = "AI",
                Text = "✅ **AI Triage Completed!** Here is the synthesized technical complaint:\n\n" +
                       $"📋 **Title**: `{session.FormattedTitle}`\n" +
                       $"🏷️ **Category**: `{session.Category}`\n" +
                       $"⚡ **Priority**: `{session.Priority}`\n" +
                       $"💻 **Diagnostics Attached**: Host `{host}`, IP `{ip}`, AnyDesk `{FleetRegistryService.Instance.LocalDevice.AnyDeskId}`\n\n" +
                       "Would you like to submit this ticket now to the IT Admin Service Desk?",
                QuickReplies = new List<string>
                {
                    "✅ Confirm & Submit IT Ticket",
                    "✏️ I want to add more details",
                    "🔄 Start Over"
                }
            };
        }
        else
        {
            if (lower.Contains("start over") || lower.Contains("restart"))
            {
                session.ConversationStep = 0;
                session.IsReadyToSubmit = false;
                return new AiChatMessage
                {
                    Sender = "AI",
                    Text = "Session reset. Please describe the problem you are experiencing.",
                    QuickReplies = new List<string>
                    {
                        "🖨️ Thermal Printer / Zebra Skipping",
                        "🌐 WMS or Internet Disconnected",
                        "⚡ Workstation Freezing or Slow"
                    }
                };
            }

            session.FormattedDescription += $"\nAdditional Note: {userText}";
            return new AiChatMessage
            {
                Sender = "AI",
                Text = "Noted. Added your note to the complaint description.\n\nReady to submit?",
                QuickReplies = new List<string> { "✅ Confirm & Submit IT Ticket", "🔄 Start Over" }
            };
        }
    }

    private static string BuildTranscriptString(AiComplaintSession session)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var m in session.Messages)
        {
            sb.AppendLine($"[{m.Timestamp:HH:mm:ss}] {m.Sender}: {m.Text}");
        }
        return sb.ToString();
    }
}

/**
 * Mock API for Chat Service
 * This provides sample responses for testing the AI chat integration
 * Remove this and implement real backend endpoints in production
 */

import { Request, Response } from 'express';

// Simple in-memory storage for demo
const sessions: any = {};
let sessionCounter = 0;

// Mock AI responses
const mockResponses = [
    "I can help you with employee management, leave applications, and HR inquiries. What would you like to know?",
    "Let me check that information for you...",
    "Based on the EMS system data, here's what I found:",
    "I understand you're asking about {topic}. Here's what I can tell you:",
    "That's a great question! Let me look into the employee records...",
    "I can help you with that. Would you like me to provide more details?",
];

function getRandomResponse(message: string): string {
    const lowerMessage = message.toLowerCase();

    if (lowerMessage.includes('leave') || lowerMessage.includes('vacation')) {
        return "I can help you with leave applications. You currently have:\n- Annual Leave: 15 days remaining\n- Sick Leave: 10 days remaining\n\nWould you like to apply for leave or check your leave history?";
    }

    if (lowerMessage.includes('employee') || lowerMessage.includes('staff')) {
        return "I can help you search employee information, view employee details, or manage employee records. What specific information are you looking for?";
    }

    if (lowerMessage.includes('policy') || lowerMessage.includes('rule')) {
        return "I can provide information about company policies including:\n- Leave policies\n- Work from home policies\n- Overtime policies\n- Performance review policies\n\nWhich policy would you like to know more about?";
    }

    if (lowerMessage.includes('hello') || lowerMessage.includes('hi')) {
        return "Hello! I'm your EMS AI assistant. I can help you with employee management, leave applications, HR policies, and system navigation. How can I assist you today?";
    }

    if (lowerMessage.includes('help')) {
        return "I can assist you with:\n\n1. *Employee Management: Search and view employee information\n2. **Leave Management: Apply for leave, check leave balance, approve leave requests\n3. **HR Policies: Get information about company policies\n4. **Reports: Generate and view HR reports\n5. **System Help*: Navigate the EMS system\n\nWhat would you like help with?";
    }

    // Default random response
    const randomIndex = Math.floor(Math.random() * mockResponses.length);
    return mockResponses[randomIndex].replace('{topic}', message);
}

export default {
    // Send a message
    'POST /api/chat/message': (req: Request, res: Response) => {
        const { message, sessionId, context } = req.body;

        setTimeout(() => {
            let currentSessionId = sessionId;

            // Create new session if none exists
            if (!currentSessionId) {
                sessionCounter++;
                currentSessionId = session - ${ sessionCounter };
                sessions[currentSessionId] = {
                    id: currentSessionId,
                    title: message.substring(0, 50) + (message.length > 50 ? '...' : ''),
                    messages: [],
                    createdAt: new Date().toISOString(),
                    updatedAt: new Date().toISOString(),
                };
            }

            // Add user message
            const userMessage = {
                id: msg - ${ Date.now()
    }- user,
role: 'user',
    content: message,
        timestamp: new Date().toISOString(),
      };

// Generate AI response
const aiMessage = {
    id: msg - ${ Date.now()}-assistant,
        role: 'assistant',
            content: getRandomResponse(message),
                timestamp: new Date().toISOString(),
                    metadata: {
    model: 'mock-gpt-4',
        tokensUsed: Math.floor(Math.random() * 100) + 50,
            mcpToolsUsed: [],
        },
      };

if (sessions[currentSessionId]) {
    sessions[currentSessionId].messages.push(userMessage, aiMessage);
    sessions[currentSessionId].updatedAt = new Date().toISOString();
}

res.json({
    succeeded: true,
    failed: false,
    data: {
        message: aiMessage,
        sessionId: currentSessionId,
        suggestions: [
            "Tell me about leave policies",
            "How can I apply for leave?",
            "Show employee directory",
        ],
    },
});
    }, 1000); // Simulate network delay
  },

// Get session
'GET /api/chat/session/:id': (req: Request, res: Response) => {
    const { id } = req.params;
    const session = sessions[id];

    if (session) {
        res.json({
            succeeded: true,
            failed: false,
            data: session,
        });
    } else {
        res.status(404).json({
            succeeded: false,
            failed: true,
            message: 'Session not found',
        });
    }
},

    // Get all sessions
    'GET /api/chat/sessions': (req: Request, res: Response) => {
        const sessionList = Object.values(sessions);
        res.json({
            succeeded: true,
            failed: false,
            data: sessionList,
        });
    },

        // Create session
        'POST /api/chat/session': (req: Request, res: Response) => {
            sessionCounter++;
            const sessionId = session - ${ sessionCounter };
            const { title } = req.body;

            sessions[sessionId] = {
                id: sessionId,
                title: title || 'New Chat',
                messages: [],
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
            };

            res.json({
                succeeded: true,
                failed: false,
                data: sessions[sessionId],
            });
        },

            // Delete session
            'DELETE /api/chat/session/:id': (req: Request, res: Response) => {
                const { id } = req.params;

                if (sessions[id]) {
                    delete sessions[id];
                    res.json({
                        succeeded: true,
                        failed: false,
                        data: true,
                    });
                } else {
                    res.status(404).json({
                        succeeded: false,
                        failed: true,
                        message: 'Session not found',
                    });
                }
            },

                // Get MCP server status
                'GET /api/chat/mcp/status': (req: Request, res: Response) => {
                    res.json({
                        succeeded: true,
                        failed: false,
                        data: [
                            {
                                name: 'File System',
                                connected: false,
                                error: 'Not configured',
                            },
                            {
                                name: 'GitHub',
                                connected: false,
                                error: 'Not configured',
                            },
                        ],
                    });
                },
};
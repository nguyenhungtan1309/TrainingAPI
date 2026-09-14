class ChatSignalRService {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.handlers = {
            onReceiveMessage: null,
            onUserTyping: null,
            onReactionUpdated: null,
            onMessageRevoked: null,
            onMessageRead: null
        };
    }

    async startConnection(token) {
        if (!token) {
            console.error("Không tìm thấy JWT Token!");
            return false;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/chathub", {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        this.connection.on("ReceiveMessage", (msg) => {
            if (this.handlers.onReceiveMessage) this.handlers.onReceiveMessage(msg);
        });

        this.connection.on("UserTyping", (data) => {
            if (this.handlers.onUserTyping) this.handlers.onUserTyping(data);
        });

        this.connection.on("ReactionUpdated", (data) => {
            if (this.handlers.onReactionUpdated) this.handlers.onReactionUpdated(data);
        });

        this.connection.on("MessageRevoked", (data) => {
            if (this.handlers.onMessageRevoked) this.handlers.onMessageRevoked(data);
        });

        this.connection.on("MessageRead", (data) => {
            if (this.handlers.onMessageRead) this.handlers.onMessageRead(data);
        });

        try {
            await this.connection.start();
            this.isConnected = true;
            console.log("Đã kết nối SignalR Hub thành công.");
            return true;
        } catch (err) {
            console.error("Lỗi khi kết nối SignalR:", err);
            this.isConnected = false;
            return false;
        }
    }

    async sendMessage(threadId, content, messageType = "Text", parentMessageId = null) {
        if (!this.isConnected) return;
        try {
            await this.connection.invoke("SendMessage", {
                threadId: threadId,
                content: content,
                messageType: messageType,
                parentMessageId: parentMessageId
            });
        } catch (err) {
            console.error("Lỗi invoke SendMessage:", err);
        }
    }

    async joinThread(threadId) {
        if (!this.isConnected) return;
        await this.connection.invoke("JoinThread", threadId);
    }

    async sendTyping(threadId, isTyping) {
        if (!this.isConnected) return;
        await this.connection.invoke("SendTyping", threadId, isTyping);
    }

    async sendReaction(threadId, messageId, reactionType) {
        if (!this.isConnected) return;
        await this.connection.invoke("SendReaction", threadId, messageId, reactionType);
    }

    async revokeMessage(threadId, messageId) {
        if (!this.isConnected) return;
        await this.connection.invoke("RevokeMessage", threadId, messageId);
    }

    async markAsRead(threadId, messageId) {
        if (!this.isConnected) return;
        await this.connection.invoke("MarkAsRead", threadId, messageId);
    }
}

const chatSignalR = new ChatSignalRService();
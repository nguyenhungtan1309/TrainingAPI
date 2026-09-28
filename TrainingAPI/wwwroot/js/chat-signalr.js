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

    /**
     * Sinh mã GUID cho mỗi lần gửi tin (ClientMessageId). Có bản dự phòng vì crypto.randomUUID chỉ
     * tồn tại ở ngữ cảnh bảo mật (https hoặc localhost) - truy cập qua http://<IP-LAN> sẽ không có.
     */
    newClientMessageId() {
        if (window.crypto && typeof window.crypto.randomUUID === "function") {
            return window.crypto.randomUUID();
        }
        return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
            const r = Math.floor(Math.random() * 16);
            return (c === "x" ? r : (r & 0x3) | 0x8).toString(16);
        });
    }

    /**
     * Gửi tin nhắn. Trả về true/false để nơi gọi biết có thành công không.
     * clientMessageId: nếu truyền lại CÙNG một mã khi gửi lại (retry), server trả về tin đã tạo
     * thay vì tạo tin trùng. attachments: [{ fileUrl, fileType, fileSize }] cho tin Image/File.
     */
    async sendMessage(threadId, content, messageType = "Text", parentMessageId = null, attachments = null, clientMessageId = null) {
        if (!this.isConnected) return false;
        try {
            await this.connection.invoke("SendMessage", {
                threadId: threadId,
                content: content,
                messageType: messageType,
                parentMessageId: parentMessageId,
                attachments: attachments,
                clientMessageId: clientMessageId || this.newClientMessageId()
            });
            return true;
        } catch (err) {
            console.error("Lỗi invoke SendMessage:", err);
            return false;
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
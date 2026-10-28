"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const bcryptjs_1 = __importDefault(require("bcryptjs"));
const store_1 = require("../store");
const auth_1 = require("../auth");
const audit_1 = require("../audit");
const router = (0, express_1.Router)();
router.post('/login', (req, res) => {
    const { username, password } = req.body;
    const clientIp = req.headers['x-forwarded-for'] || req.socket.remoteAddress || '127.0.0.1';
    if (!username || !password) {
        res.status(400).json({ error: 'Username and password are required' });
        return;
    }
    const user = store_1.store.getUser(username);
    if (!user || !bcryptjs_1.default.compareSync(password, user.passwordHash)) {
        (0, audit_1.logAudit)(username || 'UNKNOWN', 'AUTH_LOGIN_FAILED', undefined, 'Invalid credentials', clientIp, 'FAILED');
        res.status(401).json({ error: 'Invalid username or password' });
        return;
    }
    const token = (0, auth_1.generateToken)({
        id: user.id,
        username: user.username,
        role: user.role,
    });
    (0, audit_1.logAudit)(user.username, 'AUTH_LOGIN_SUCCESS', undefined, `Admin logged in with role: ${user.role}`, clientIp, 'SUCCESS');
    res.json({
        token,
        user: {
            id: user.id,
            username: user.username,
            role: user.role,
        },
    });
});
router.get('/me', auth_1.requireAuth, (req, res) => {
    res.json({ user: req.user });
});
exports.default = router;

"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.JWT_SECRET = void 0;
exports.generateToken = generateToken;
exports.verifyJwtToken = verifyJwtToken;
exports.requireAuth = requireAuth;
exports.requireRole = requireRole;
const jsonwebtoken_1 = __importDefault(require("jsonwebtoken"));
exports.JWT_SECRET = process.env.JWT_SECRET || 'MESH_SECRET_ENTERPRISE_KEY_99420_X';
function generateToken(payload) {
    return jsonwebtoken_1.default.sign(payload, exports.JWT_SECRET, { expiresIn: '12h' });
}
function verifyJwtToken(token) {
    try {
        return jsonwebtoken_1.default.verify(token, exports.JWT_SECRET);
    }
    catch {
        return null;
    }
}
function requireAuth(req, res, next) {
    const authHeader = req.headers.authorization;
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
        res.status(401).json({ error: 'Authorization header missing or invalid format' });
        return;
    }
    const token = authHeader.substring(7);
    const decoded = verifyJwtToken(token);
    if (!decoded) {
        res.status(401).json({ error: 'Invalid or expired token' });
        return;
    }
    req.user = decoded;
    next();
}
function requireRole(allowedRoles) {
    return (req, res, next) => {
        if (!req.user || !allowedRoles.includes(req.user.role)) {
            res.status(403).json({ error: 'Access forbidden: Insufficient permissions' });
            return;
        }
        next();
    };
}

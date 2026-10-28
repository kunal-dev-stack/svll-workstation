"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const store_1 = require("../store");
const auth_1 = require("../auth");
const router = (0, express_1.Router)();
router.use(auth_1.requireAuth);
router.get('/', (0, auth_1.requireRole)(['superadmin', 'admin', 'auditor']), (req, res) => {
    const limit = parseInt(req.query.limit, 10) || 100;
    const logs = store_1.store.getAuditLogs(limit);
    res.json(logs);
});
exports.default = router;

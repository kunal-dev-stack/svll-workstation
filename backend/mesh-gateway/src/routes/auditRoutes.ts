import { Router, Response } from 'express';
import { store } from '../store';
import { requireAuth, requireRole, AuthenticatedRequest } from '../auth';

const router = Router();

router.use(requireAuth);

router.get('/', requireRole(['superadmin', 'admin', 'auditor']), (req: AuthenticatedRequest, res: Response): void => {
  const limit = parseInt(req.query.limit as string, 10) || 100;
  const logs = store.getAuditLogs(limit);
  res.json(logs);
});

export default router;

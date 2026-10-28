import { Router, Request, Response } from 'express';
import bcrypt from 'bcryptjs';
import { store } from '../store';
import { generateToken, requireAuth, AuthenticatedRequest } from '../auth';
import { logAudit } from '../audit';

const router = Router();

router.post('/login', (req: Request, res: Response): void => {
  const { username, password } = req.body;
  const clientIp = (req.headers['x-forwarded-for'] as string) || req.socket.remoteAddress || '127.0.0.1';

  if (!username || !password) {
    res.status(400).json({ error: 'Username and password are required' });
    return;
  }

  const user = store.getUser(username);
  if (!user || !bcrypt.compareSync(password, user.passwordHash)) {
    logAudit(username || 'UNKNOWN', 'AUTH_LOGIN_FAILED', undefined, 'Invalid credentials', clientIp, 'FAILED');
    res.status(401).json({ error: 'Invalid username or password' });
    return;
  }

  const token = generateToken({
    id: user.id,
    username: user.username,
    role: user.role,
  });

  logAudit(user.username, 'AUTH_LOGIN_SUCCESS', undefined, `Admin logged in with role: ${user.role}`, clientIp, 'SUCCESS');

  res.json({
    token,
    user: {
      id: user.id,
      username: user.username,
      role: user.role,
    },
  });
});

router.get('/me', requireAuth, (req: AuthenticatedRequest, res: Response): void => {
  res.json({ user: req.user });
});

export default router;

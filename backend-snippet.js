const express = require('express');
const router = express.Router();

// Simulação de banco de dados
// Em produção, substitua por chamadas reais ao seu DB (Postgres, Mongo, etc.)
const usersDB = [
    { email: 'demo@coincraft.com', hardwareIds: [], maxInstalls: 3 }
];

// Função auxiliar para gerar chave (Simulação)
// Na prática, use sua lógica de criptografia/assinatura (RSA/ECDSA) compatível com o app C#
function generateSignedLicense(email, hardwareId) {
    // Exemplo fictício: Retorna uma string que o app C# vai validar
    // O app C# espera que isso seja uma licença válida (XML assinado ou string verificável)
    return `LICENSE-FOR-${email}-${hardwareId.substring(0, 6)}-${Date.now()}`; 
}

/**
 * Rota: POST /api/licenses/activate-by-email
 * Body: { "email": "user@example.com", "hardwareId": "ABC-123-XYZ" }
 */
router.post('/activate-by-email', async (req, res) => {
    try {
        const { email, hardwareId } = req.body;

        if (!email || !hardwareId) {
            return res.status(400).json({ message: 'E-mail e HardwareID são obrigatórios.' });
        }

        // 1. Verificar se o usuário possui uma licença válida
        const user = usersDB.find(u => u.email.toLowerCase() === email.toLowerCase());

        if (!user) {
            return res.status(404).json({ message: 'E-mail não encontrado ou sem licença ativa.' });
        }

        // 2. Verificar se este hardware já está ativado para este usuário
        const existingHw = user.hardwareIds.find(id => id === hardwareId);

        if (existingHw) {
            // Máquina já autorizada, retorna a licença correspondente (idempotência)
            const licenseKey = generateSignedLicense(email, hardwareId);
            return res.json({ licenseKey });
        }

        // 3. Se é uma máquina nova, verificar se excedeu o limite
        if (user.hardwareIds.length >= user.maxInstalls) {
            return res.status(403).json({ 
                message: `Limite de ativações excedido. Seu plano permite ${user.maxInstalls} máquinas.` 
            });
        }

        // 4. Registrar nova ativação
        user.hardwareIds.push(hardwareId);
        
        // Persistir no banco de dados...
        // await db.save(user);

        // 5. Gerar e retornar a licença
        const licenseKey = generateSignedLicense(email, hardwareId);

        return res.json({ licenseKey });

    } catch (error) {
        console.error('Erro na ativação:', error);
        return res.status(500).json({ message: 'Erro interno no servidor.' });
    }
});

module.exports = router;

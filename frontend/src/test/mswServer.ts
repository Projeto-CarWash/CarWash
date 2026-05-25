import { setupServer } from 'msw/node';

import { handlersPadrao } from './handlers';

/**
 * Servidor MSW (Node) usado nos testes. Inicia com os handlers de "caminho
 * feliz"; testes individuais sobrescrevem com `server.use(...)` para simular
 * erros (409, 422, etc.).
 */
export const server = setupServer(...handlersPadrao);

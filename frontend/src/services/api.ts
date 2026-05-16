import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

// Interceptor — injeta Bearer token em todas as requisições
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('carwash_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: unknown) => Promise.reject(error),
);

// Interceptor de resposta — trata 401 (token expirado)
api.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      localStorage.removeItem('carwash_token');
      localStorage.removeItem('carwash_user');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  },
);

export default api;

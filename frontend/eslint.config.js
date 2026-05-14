// ESLint flat config — CarWash frontend
// Cobre: TS strict, React 19, Hooks, Vite Refresh, A11y (RNF008), import order, Prettier interop.
import js from '@eslint/js';
import globals from 'globals';
import tseslint from 'typescript-eslint';
import react from 'eslint-plugin-react';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import jsxA11y from 'eslint-plugin-jsx-a11y';
import importX from 'eslint-plugin-import-x';
import { createTypeScriptImportResolver } from 'eslint-import-resolver-typescript';
import prettierConfig from 'eslint-config-prettier';
import { defineConfig, globalIgnores } from 'eslint/config';

export default defineConfig([
  globalIgnores([
    'dist',
    'build',
    'coverage',
    'node_modules',
    'playwright-report',
    'test-results',
    '.vite',
  ]),

  // Código de aplicação
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommendedTypeChecked,
      tseslint.configs.stylisticTypeChecked,
      react.configs.flat.recommended,
      react.configs.flat['jsx-runtime'],
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
      jsxA11y.flatConfigs.recommended,
      importX.flatConfigs.recommended,
      importX.flatConfigs.typescript,
      prettierConfig,
    ],
    languageOptions: {
      ecmaVersion: 2023,
      sourceType: 'module',
      globals: { ...globals.browser, ...globals.es2023 },
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    settings: {
      react: { version: 'detect' },
      'import-x/resolver-next': [
        createTypeScriptImportResolver({ project: './tsconfig.app.json' }),
        importX.createNodeResolver(),
      ],
    },
    rules: {
      // TypeScript strict
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/consistent-type-imports': ['error', { prefer: 'type-imports' }],
      '@typescript-eslint/no-misused-promises': [
        'error',
        { checksVoidReturn: { attributes: false } },
      ],
      '@typescript-eslint/no-floating-promises': 'error',

      // React
      'react/jsx-uses-react': 'off', // React 17+ JSX transform
      'react/react-in-jsx-scope': 'off', // idem
      'react/prop-types': 'off', // usamos TS
      'react/self-closing-comp': 'error',
      'react/jsx-curly-brace-presence': ['warn', { props: 'never', children: 'never' }],

      // Hooks
      'react-hooks/rules-of-hooks': 'error',
      'react-hooks/exhaustive-deps': 'error',

      // Acessibilidade — RNF008
      'jsx-a11y/anchor-is-valid': 'error',
      'jsx-a11y/click-events-have-key-events': 'warn',
      'jsx-a11y/no-static-element-interactions': 'warn',

      // Imports — organização e detecção de circulares
      'import-x/order': [
        'error',
        {
          groups: ['builtin', 'external', 'internal', 'parent', 'sibling', 'index', 'type'],
          'newlines-between': 'always',
          alphabetize: { order: 'asc', caseInsensitive: true },
        },
      ],
      'import-x/no-cycle': ['error', { maxDepth: 4 }],
      'import-x/no-self-import': 'error',

      // Higiene geral
      'no-console': ['warn', { allow: ['warn', 'error'] }],
      'no-debugger': 'error',
      eqeqeq: ['error', 'always'],
    },
  },

  // Arquivos de config (vite.config, eslint.config, etc.) — relaxar regras que exigem TS project
  {
    files: ['*.config.{js,ts,mjs,cjs}', 'vite.config.ts'],
    languageOptions: {
      globals: { ...globals.node },
      parserOptions: { project: null },
    },
    rules: {
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',
    },
  },

  // Testes — relaxar para permitir non-null assertion e expects encadeados
  {
    files: ['**/*.{test,spec}.{ts,tsx}', '**/__tests__/**/*.{ts,tsx}'],
    languageOptions: { globals: { ...globals.node, ...globals.browser } },
    rules: {
      '@typescript-eslint/no-non-null-assertion': 'off',
      '@typescript-eslint/no-explicit-any': 'off',
      'no-console': 'off',
    },
  },
]);

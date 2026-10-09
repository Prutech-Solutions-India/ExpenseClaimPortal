import globals from 'globals';
import tseslint from 'typescript-eslint';
import reactHooks from 'eslint-plugin-react-hooks';

export default tseslint.config(
  // bin/, obj/ and wwwroot/ are .NET build output; wwwroot in particular holds
  // the minified bundle, which would otherwise be linted as source.
  {
    ignores: [
      'coverage',
      'reports',
      'node_modules',
      '**/bin/**',
      '**/obj/**',
      'src/PilotService/wwwroot/**',
      'publish',
    ],
  },
  ...tseslint.configs.recommended,
  {
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2022,
      globals: { ...globals.browser, ...globals.node },
    },
    plugins: {
      'react-hooks': reactHooks,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
    },
  },
);

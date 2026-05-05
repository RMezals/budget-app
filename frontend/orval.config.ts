import { defineConfig } from 'orval';

export default defineConfig({
  budgetapp: {
    input: {
      target: 'http://localhost:5000/swagger/v1/swagger.json',
    },
    output: {
      mode: 'tags-split',
      target: './src/api/generated',
      client: 'fetch',
      baseUrl: 'http://localhost:5000',
      override: {
        mutator: {
          path: './src/api/client.ts',
          name: 'apiFetch',
        },
      },
    },
  },
});

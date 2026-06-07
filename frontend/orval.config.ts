import { defineConfig } from 'orval';

export default defineConfig({
  budgetApp: {
    input: '../swagger.json',
    output: {
      client: 'zod',
      target: './src/api/generated/schemas.ts',
      fileExtension: '.ts',
    },
  },
});

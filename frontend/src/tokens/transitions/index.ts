export interface TransitionsType {
  fast: string;
  base: string;
  slow: string;
  slower: string;
  spring: string;
}

export const Transitions: TransitionsType = {
  fast: '150ms ease',
  base: '200ms ease',
  slow: '300ms ease',
  slower: '500ms ease',
  spring: '300ms cubic-bezier(0.34, 1.56, 0.64, 1)',
};

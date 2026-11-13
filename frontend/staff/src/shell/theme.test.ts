import { describe, expect, it } from 'vitest';
import { parseChoice, themeFor } from './theme';

describe('parseChoice', () => {
  it('reads a saved choice', () => {
    expect(parseChoice('limestone')).toBe('limestone');
    expect(parseChoice('night')).toBe('night');
  });

  it('follows the device when nothing, or something unknown, is saved', () => {
    expect(parseChoice(null)).toBe('system');
    expect(parseChoice('system')).toBe('system');
    expect(parseChoice('dark')).toBe('system');
  });
});

describe('themeFor', () => {
  it('keeps Light and Dark whatever the device says', () => {
    expect(themeFor('limestone', true)).toBe('limestone');
    expect(themeFor('night', false)).toBe('night');
  });

  it('follows the device for System', () => {
    expect(themeFor('system', true)).toBe('night');
    expect(themeFor('system', false)).toBe('limestone');
  });
});

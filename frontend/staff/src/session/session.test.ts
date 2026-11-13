import { describe, expect, it } from 'vitest';
import { initials, landingFor, loginPath, safeReturnTo } from './session';

describe('landingFor', () => {
  it('opens the office on its new leads and a technician on their own work', () => {
    expect(landingFor('Admin')).toBe('/leads');
    expect(landingFor('Technician')).toBe('/my-work');
  });
});

describe('safeReturnTo', () => {
  it('keeps an address inside the app, with its query', () => {
    expect(safeReturnTo('/installations/12')).toBe('/installations/12');
    expect(safeReturnTo('/leads?status=new&page=2')).toBe('/leads?status=new&page=2');
  });

  it.each([
    ['//elsewhere.example/phish'],
    ['/\\elsewhere.example'],
    ['https://elsewhere.example'],
    ['javascript:alert(1)'],
    ['leads'],
    [''],
  ])('refuses %j: it could leave the app', value => {
    expect(safeReturnTo(value)).toBeNull();
  });

  it('refuses the sign-in page itself', () => {
    expect(safeReturnTo('/login')).toBeNull();
    expect(safeReturnTo('/login?returnTo=%2Fleads')).toBeNull();
  });

  it('has nothing to keep without a value', () => {
    expect(safeReturnTo(null)).toBeNull();
  });
});

describe('loginPath', () => {
  it('remembers the page, encoded', () => {
    expect(loginPath('/installations/12?tab=history')).toBe('/login?returnTo=%2Finstallations%2F12%3Ftab%3Dhistory');
  });

  it("doesn't remember the app's root", () => {
    expect(loginPath('/')).toBe('/login');
  });
});

describe('initials', () => {
  it.each([
    ['Rosa Castillo', 'RC'],
    ['Jamal Ortiz', 'JO'],
    ['Mary Ann de la Cruz', 'MC'],
    ['  priya   raman ', 'PR'],
    ['Cher', 'C'],
    ['Élodie Ångström', 'ÉÅ'],
    ['', '?'],
  ])('%j → %j', (name, expected) => {
    expect(initials(name)).toBe(expected);
  });
});

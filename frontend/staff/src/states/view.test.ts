import { describe, expect, it } from 'vitest';
import { ApiError, MESSAGES } from '../api/errors';
import { viewOf } from './view';

const notFound = new ApiError('notFound', MESSAGES.notFound, { status: 404 });
const unreachable = new ApiError('unreachable', MESSAGES.unreachable);
const isEmpty = (rows: string[]) => rows.length === 0;

describe('viewOf', () => {
  it('is loading until the first answer', () => {
    expect(viewOf({ status: 'pending', error: null, data: undefined })).toBe('loading');
  });

  it('shows the data, or the empty state when there is none', () => {
    expect(viewOf({ status: 'success', error: null, data: ['Dana Whitfield'] }, isEmpty)).toBe('ready');
    expect(viewOf({ status: 'success', error: null, data: [] }, isEmpty)).toBe('empty');
    expect(viewOf({ status: 'success', error: null, data: [] })).toBe('ready');
  });

  it('is not found for a 404, failed for anything else', () => {
    expect(viewOf({ status: 'error', error: notFound, data: undefined })).toBe('notFound');
    expect(viewOf({ status: 'error', error: unreachable, data: undefined })).toBe('failed');
    expect(viewOf({ status: 'error', error: new TypeError('x is undefined'), data: undefined })).toBe('failed');
  });

  it('keeps the data shown when a later refresh fails', () => {
    expect(viewOf({ status: 'error', error: unreachable, data: ['Dana Whitfield'] }, isEmpty)).toBe('ready');
  });
});

/** A list's page from its address (`?page=2`): a whole number from 1, or the first page. */
export const pageFrom = (value: string | null) => {
  const page = Number(value);
  return Number.isInteger(page) && page >= 1 ? page : 1;
};

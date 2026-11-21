/** A record's ID from its address (`/leads/:id`), if it is one: a whole number from 1, written plainly. */
export const idFrom = (value: string | undefined) => {
  const id = Number(value);
  return Number.isInteger(id) && id >= 1 && String(id) === value ? id : null;
};

import { z } from 'zod';

/*
 * The design card's own checks, before anything is sent: the API's limits, which catch a typo (840 for 8.4), not a
 * business rule. The values stay text in the form, and are sent as numbers.
 */
export const designSchema = z.object({
  systemSizeKw: z.string().trim()
    .regex(/^\d{1,3}(\.\d{1,2})?$/, { message: 'Enter the size in kW, such as 8.4.', abort: true })
    .refine(v => Number(v) >= 0.5 && Number(v) <= 100, 'The size is between 0.5 and 100 kW.'),
  panelCount: z.string().trim()
    .regex(/^\d{1,3}$/, { message: 'Enter a whole number of panels, such as 21.', abort: true })
    .refine(v => Number(v) >= 1 && Number(v) <= 400, 'The count is between 1 and 400 panels.'),
  inverterType: z.string().trim().max(100, "The inverter can't be longer than 100 characters."),
});
export type DesignValues = z.infer<typeof designSchema>;

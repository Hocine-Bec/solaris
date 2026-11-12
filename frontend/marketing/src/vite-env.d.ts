/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** The API's base URL, including /api */
  readonly VITE_API_BASE_URL?: string;
  /** A Geoapify key for the address suggestions; without one, the street field is a plain field */
  readonly VITE_GEOAPIFY_API_KEY?: string;
}

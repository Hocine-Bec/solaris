import { Hero } from './Hero';
import { GetStarted, HowItWorks, Idea, Statement, TopQuestions, WhatYouGet, WhoItsFor } from './HomeSections';

/** The home page: one long scroll, alternating Night and Limestone sections. */
export function HomePage() {
  return (
    <>
      <Hero />
      <Idea />
      <HowItWorks />
      <WhatYouGet />
      <WhoItsFor />
      <Statement />
      <TopQuestions />
      <GetStarted />
    </>
  );
}

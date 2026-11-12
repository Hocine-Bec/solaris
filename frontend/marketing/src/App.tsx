import { useEffect, useLayoutEffect, useRef } from 'react';
import { Route, Routes, useLocation } from 'react-router-dom';
import { Footer } from './components/Footer';
import { Header } from './components/Header';
import { pageFor } from './content/pages';
import { playIntro } from './lib/intro';
import { applyPageMeta } from './lib/pageMeta';
import { AboutPage } from './pages/AboutPage';
import { HomePage } from './pages/home/HomePage';
import { HowItWorksPage } from './pages/HowItWorksPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { PrivacyPage } from './pages/PrivacyPage';
import { QuestionsPage } from './pages/QuestionsPage';
import { QuotePage } from './quote/QuotePage';
import { QuoteSentPage } from './quote/QuoteSentPage';

export default function App() {
  const logoRef = useRef<SVGSVGElement>(null);
  const mainRef = useRef<HTMLElement>(null);
  const { pathname } = useLocation();
  const firstPage = useRef(true);

  useEffect(() => { if (logoRef.current) playIntro(logoRef.current); }, []);

  // Each page: its title and description; a new page starts at the top, with the focus on it (not on the first load).
  useLayoutEffect(() => {
    applyPageMeta(pageFor(pathname));
    if (firstPage.current) { firstPage.current = false; return; }
    scrollTo(0, 0);
    mainRef.current?.focus({ preventScroll: true });
  }, [pathname]);

  // Sections fade and rise once, as they enter.
  useEffect(() => {
    const seen = new IntersectionObserver(entries => {
      for (const entry of entries) if (entry.isIntersecting) { entry.target.classList.add('in'); seen.unobserve(entry.target); }
    }, { rootMargin: '0px 0px -12% 0px' });
    mainRef.current?.querySelectorAll('.reveal').forEach(el => seen.observe(el));
    return () => seen.disconnect();
  }, [pathname]);

  return (
    <>
      <Header logoRef={logoRef} mainRef={mainRef} />
      <main id="main" tabIndex={-1} ref={mainRef}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/how-it-works" element={<HowItWorksPage />} />
          <Route path="/questions" element={<QuestionsPage />} />
          <Route path="/about" element={<AboutPage />} />
          <Route path="/quote" element={<QuotePage />} />
          <Route path="/quote/sent" element={<QuoteSentPage />} />
          <Route path="/privacy" element={<PrivacyPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </main>
      <Footer />
    </>
  );
}

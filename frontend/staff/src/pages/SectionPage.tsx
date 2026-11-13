import { Icon } from '../components/Icon';
import { PageHead } from '../components/PageHead';
import type { Section } from '../shell/sections';

/** A section whose screen comes with its feature: its title, and what it will hold. */
export function SectionPage({ section }: { section: Section }) {
  return (
    <>
      <PageHead title={section.title} />
      <div className="state-box">
        <div className="state">
          <span className="ring"><Icon icon={section.icon} /></span>
          <p className="a-heading">Not built yet</p>
          <p>{section.holds}</p>
        </div>
      </div>
    </>
  );
}

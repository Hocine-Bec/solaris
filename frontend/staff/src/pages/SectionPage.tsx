import { Icon } from '../components/Icon';
import { PageHead } from '../components/PageHead';
import { useCurrentUser } from '../session/session';
import { opensFor, type Section } from '../shell/sections';
import { NotAllowed } from '../states/States';

/**
 * A section whose screen comes with its feature: its title, and what it will hold. Another role's section says so
 * instead (a technician on an office page, an Admin on My work).
 */
export function SectionPage({ section }: { section: Section }) {
  const { role } = useCurrentUser();
  if (!opensFor(role, section.path)) return <NotAllowed />;
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

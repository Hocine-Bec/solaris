interface StatCardProps {
  value: string;
  label: string;
}

export default function StatCard({ value, label }: StatCardProps) {
  return (
    <div className="bg-white/80 dark:bg-black/20 backdrop-blur-sm p-6 rounded-xl border border-primary/40 dark:border-primary/20 hover:border-primary/60 dark:hover:border-primary/30 transition-all duration-300 hover:shadow-lg hover:shadow-primary/20 dark:hover:shadow-primary/10">
      <p className="text-3xl font-bold text-gray-900 dark:text-white">{value}</p>
      <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">{label}</p>
    </div>
  );
}
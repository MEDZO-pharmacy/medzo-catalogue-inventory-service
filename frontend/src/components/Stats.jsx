import { Award, Pill, UserRoundCheck, Users } from 'lucide-react';

const Stats = () => {
  const statCards = [
    {
      id: 1,
      icon: <Award size={32} className="text-[#0a192f]" />,
      number: '15',
      label: 'Years of Experience'
    },
    {
      id: 2,
      icon: <Pill size={32} className="text-[#0a192f]" />,
      number: '500+',
      label: 'Kind of Medicines'
    },
    {
      id: 3,
      icon: <UserRoundCheck size={32} className="text-[#0a192f]" />,
      number: '150+',
      label: 'Professional Staff'
    },
    {
      id: 4,
      icon: <Users size={32} className="text-[#0a192f]" />,
      number: '200+',
      label: 'Active customers'
    }
  ];

  return (
    <section className="bg-[#f0f5fc] py-20 px-12 border-l-4 border-r-4 border-medzo-blue border-opacity-30">
      <div className="max-w-4xl mx-auto text-center space-y-4 mb-16">
        <h2 className="text-4xl font-bold text-[#0a192f] leading-tight">
          All Your Essential Medicines and Vitamins, <br />in One Place
        </h2>
        <p className="text-gray-600 text-lg">
          Discover a complete range of trusted medicines and essential vitamins, all in one place.<br className="hidden md:block" />
          Shop quality healthcare products designed to support your everyday health and wellness with convenience and confidence.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8 max-w-7xl mx-auto">
        {statCards.map(stat => (
          <div key={stat.id} className="bg-white rounded-2xl p-8 flex flex-col items-center text-center shadow-[0_8px_30px_rgb(0,0,0,0.04)] hover:shadow-[0_8px_30px_rgb(0,0,0,0.08)] transition-shadow">
            <div className="bg-[#f0f4f8] p-4 rounded-2xl mb-6">
              {stat.icon}
            </div>
            <h3 className="text-5xl font-bold text-medzo-green mb-3">
              {stat.number}
            </h3>
            <p className="text-[#4a5568] font-bold text-lg">
              {stat.label}
            </p>
          </div>
        ))}
      </div>
    </section>
  );
};

export default Stats;

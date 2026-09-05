import { MapPin, Phone, Mail } from 'lucide-react';

const Footer = () => {
  return (
    <footer className="bg-[#ebf3ff] pt-16 pb-12 px-6 md:px-12">
      <div className="w-full max-w-[1400px] mx-auto grid grid-cols-1 md:grid-cols-4 gap-8">

        {/* Brand */}
        <div className="col-span-1">
          <div className="flex items-center gap-2 text-[#0a192f] font-bold text-2xl mb-1 cursor-pointer">
            <img
              src="/hospital-icon.svg"
              alt="Medzo Logo"
              className="w-6 h-6"
            />
            Medzo
          </div>
          <p className="text-[#6b7280] text-sm font-medium ml-8">
            Medical Pharmacy
          </p>
        </div>

        {/* Other Pages */}
        <div className="col-span-1">
          <h4 className="font-bold text-[#0a192f] mb-6">Other Pages</h4>
          <ul className="space-y-4 text-sm text-[#4a5568] font-medium">
            <li><a href="#" className="hover:text-blue-600 transition-colors">Privacy Policy</a></li>
            <li><a href="#" className="hover:text-blue-600 transition-colors">Terms of Service</a></li>
            <li><a href="#" className="hover:text-blue-600 transition-colors">Customer Care</a></li>
            <li><a href="#" className="hover:text-blue-600 transition-colors">Careers</a></li>
            <li><a href="#" className="hover:text-blue-600 transition-colors">Emergency Support</a></li>
          </ul>
        </div>

        {/* Contact Info */}
        <div className="col-span-1">
          <h4 className="font-bold text-[#0a192f] mb-6">Contact Info</h4>
          <ul className="space-y-6 text-sm text-[#4a5568] font-medium">
            <li className="flex items-start gap-3">
              <MapPin size={20} className="text-[#0a192f] shrink-0" />
              <span>Colombo 10, Nugegoda, Kadawatha</span>
            </li>
            <li className="flex items-center gap-3">
              <Phone size={20} className="text-[#0a192f] shrink-0" />
              <span>+94 763 244 890</span>
            </li>
            <li className="flex items-center gap-3">
              <Mail size={20} className="text-[#0a192f] shrink-0" />
              <span>medzopharmacy@gmail.com</span>
            </li>
          </ul>
        </div>

        {/* Copyright */}
        <div className="col-span-1 flex items-start md:justify-end">
          <p className="text-sm text-[#4a5568] font-medium mt-1 md:text-right">
            © 2026 Medzo Healthcare. All rights reserved.
          </p>
        </div>

      </div>
    </footer>
  );
};

export default Footer;

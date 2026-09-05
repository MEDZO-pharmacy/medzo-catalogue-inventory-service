import { Link } from 'react-router-dom';

const Hero = () => {
  return (
    <section className="flex flex-col lg:flex-row items-center justify-between px-12 py-16 gap-12 bg-white">
      {/* Left Content */}
      <div className="flex-1 space-y-8 max-w-2xl">
        {/* Badge */}
        <div className="inline-flex items-center gap-2 bg-[#f0f4f8] text-[#6b7280] px-4 py-2 rounded-full text-sm font-semibold tracking-wide border border-gray-200">
          <span className="w-2 h-2 rounded-full bg-medzo-blue"></span>
          MODERN HEALTHCARE PRODUCTS
        </div>

        {/* Heading */}
        <h1 className="text-5xl lg:text-6xl font-bold leading-tight text-[#0a192f]">
          Provide Best Quality<br />
          <span className="text-[#0a192f]">MEDICINE</span>
        </h1>

        {/* Description */}
        <p className="text-lg text-gray-600 leading-relaxed max-w-xl">
          Experience better, more responsible pharmacy care. We are committed to providing quality medical products and trusted service for everyone, making your healthcare journey safer, easier, and more convenient.
        </p>

        {/* Action Buttons */}
        <div className="flex items-center gap-4 pt-4">
          <Link to="/products" className="gradient-btn text-white px-8 py-3.5 rounded-lg font-semibold hover:opacity-90 transition-opacity shadow-md shadow-blue-200 inline-block text-center">
            Products
          </Link>
          <Link to="/read-more" className="bg-white text-[#0a192f] border border-gray-300 px-8 py-3.5 rounded-lg font-semibold hover:bg-gray-50 transition-colors shadow-sm inline-block text-center">
            Read More
          </Link>
        </div>
      </div>

      {/* Right Image */}
      <div className="flex-1 w-full flex justify-end">
        <div className="relative w-full max-w-2xl h-[500px] rounded-2xl overflow-hidden shadow-2xl">
          <img
            src="/image.png"
            alt="Pharmacy Staff"
            className="w-full h-full object-cover"
          />
        </div>
      </div>
    </section>
  );
};

export default Hero;

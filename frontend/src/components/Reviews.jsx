import { useState, useEffect, useCallback } from 'react';
import { Star, Quote, ChevronLeft, ChevronRight, PenSquare, X } from 'lucide-react';
import { ApiError, createReview, getReviews } from '../services/authApi';

const initialReviews = [
  {
    id: 1,
    name: "Jagath Bandara",
    role: "Regular Customer",
    rating: 5,
    comment: "Medzo provides quality medicines at reasonable prices. I feel confident knowing that I am getting genuine and reliable products"
  },
  {
    id: 2,
    name: "Priyangani Silva",
    role: "Regular Customer",
    rating: 5,
    comment: "The service was quick and efficient. The staff handled my order professionally and made sure everything was correct before I left"
  },
  {
    id: 3,
    name: "Harshana Vithanage",
    role: "Regular Customer",
    rating: 5,
    comment: "I really appreciate the staff's attention to detail. They provided the right medicine and clearly explained how to use it. Very responsible service."
  }
];

const Reviews = () => {
  const [reviews, setReviews] = useState(initialReviews);
  const [index, setIndex] = useState(0);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [submitError, setSubmitError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [formData, setFormData] = useState({
    name: '',
    role: 'Regular Customer',
    rating: 5,
    comment: ''
  });

  const handleNext = useCallback(() => {
    setIndex((prevIndex) => (prevIndex + 1) % reviews.length);
  }, [reviews.length]);

  useEffect(() => {
    let active = true;
    getReviews()
      .then((items) => {
        if (active && items.length > 0) {
          setReviews(items.map((review) => ({ ...review, role: review.customerType })));
          setIndex(0);
        }
      })
      .catch(() => {});
    return () => { active = false; };
  }, []);

  const handlePrev = useCallback(() => {
    setIndex((prevIndex) => (prevIndex - 1 + reviews.length) % reviews.length);
  }, [reviews.length]);

  useEffect(() => {
    const handleKeyDown = (event) => {
      if (isModalOpen) return;
      if (event.key === 'ArrowRight') handleNext();
      if (event.key === 'ArrowLeft') handlePrev();
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [handleNext, handlePrev, isModalOpen]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.comment.trim()) return;

    setIsSubmitting(true);
    setSubmitError('');
    try {
      const saved = await createReview({
        name: formData.name.trim(),
        customerType: formData.role,
        rating: formData.rating,
        comment: formData.comment.trim(),
      });
      setReviews((current) => [{ ...saved, role: saved.customerType }, ...current]);
      setIndex(0);
      setIsModalOpen(false);
      setFormData({ name: '', role: 'Regular Customer', rating: 5, comment: '' });
    } catch (error) {
      setSubmitError(error instanceof ApiError ? error.message : 'The review could not be saved.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const currentReview = reviews[index];

  return (
    <section className="py-24 px-6 md:px-12 bg-white">
      <div className="w-full max-w-[1400px] mx-auto">
        <div className="bg-gradient-to-br from-[#e6f0ff] to-[#d6e5ff] rounded-3xl min-h-[500px] flex flex-col items-center justify-between p-8 md:p-14 text-center relative shadow-sm">

          {/* Header & Write Review Button */}
          <div className="flex flex-col items-center gap-4 w-full">
            <h2 className="text-3xl md:text-4xl font-bold text-[#0a192f]">
              Customer Reviews
            </h2>
            <p className="text-[#4a5568] text-sm md:text-base max-w-lg">
              See What Our Customers Have to Say
            </p>

            {/* Modal Trigger Button */}
            <button
              onClick={() => setIsModalOpen(true)}
              className="mt-2 inline-flex items-center gap-2 gradient-btn text-white text-sm font-semibold py-2.5 px-5 rounded-full transition-all shadow-sm active:scale-95 cursor-pointer"
            >
              <PenSquare size={16} />
              <span>Write a Review</span>
            </button>
          </div>

          {/* Review Display Card */}
          <div className="relative my-6 max-w-2xl w-full">
            <button
              onClick={handlePrev}
              className="absolute -left-4 md:-left-12 top-1/2 -translate-y-1/2 p-3 rounded-full bg-white text-[#0a192f] shadow-md hover:bg-gray-50 focus:outline-none transition-transform active:scale-95 z-10"
              aria-label="Previous review"
            >
              <ChevronLeft size={24} />
            </button>

            <div className="bg-white/80 backdrop-blur-md p-8 rounded-2xl shadow-md transition-all duration-300">
              <Quote className="w-10 h-10 text-[#0a192f]/20 mx-auto mb-4" />

              <div className="flex justify-center gap-1 mb-4">
                {[...Array(currentReview.rating)].map((_, i) => (
                  <Star key={i} size={18} className="fill-amber-400 text-amber-400" />
                ))}
              </div>

              <p className="text-[#0a192f] text-lg md:text-xl font-medium leading-relaxed mb-6 min-h-[80px] flex items-center justify-center">
                "{currentReview.comment}"
              </p>

              <div>
                <h4 className="font-bold text-[#0a192f] text-base">{currentReview.name}</h4>
                <span className="text-xs text-[#6b7280] font-medium">{currentReview.role}</span>
              </div>
            </div>

            <button
              onClick={handleNext}
              className="absolute -right-4 md:-right-12 top-1/2 -translate-y-1/2 p-3 rounded-full bg-white text-[#0a192f] shadow-md hover:bg-gray-50 focus:outline-none transition-transform active:scale-95 z-10"
              aria-label="Next review"
            >
              <ChevronRight size={24} />
            </button>
          </div>

          {/* Navigation Dots */}
          <div className="flex gap-2 justify-center items-center">
            {reviews.map((_, i) => (
              <button
                key={i}
                onClick={() => setIndex(i)}
                className={`h-2.5 rounded-full transition-all duration-300 ${index === i ? "w-8 bg-[#0a192f]" : "w-2.5 bg-[#a0aec0] hover:bg-gray-400"
                  }`}
                aria-label={`Go to review ${i + 1}`}
              />
            ))}
          </div>

        </div>
      </div>

      {/* Pop-Up Review Form Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg p-6 md:p-8 relative animate-in fade-in zoom-in-95 duration-200">

            {/* Modal Header */}
            <div className="flex justify-between items-center mb-6 border-b pb-3">
              <h3 className="text-2xl font-bold text-[#0a192f]">Share Your Experience</h3>
              <button
                onClick={() => setIsModalOpen(false)}
                className="text-gray-400 hover:text-gray-600 transition-colors"
              >
                <X size={24} />
              </button>
            </div>

            {/* Review Form */}
            <form onSubmit={handleSubmit} className="space-y-5 text-left">
              {submitError && <div role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{submitError}</div>}

              {/* Name Input */}
              <div>
                <label className="block text-sm font-semibold text-[#0a192f] mb-1">
                  Full Name
                </label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Nimal Perera"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full px-4 py-2.5 bg-[#f8fafc] border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#0a192f] text-sm"
                />
              </div>

              {/* Customer Type Options */}
              <div>
                <label className="block text-sm font-semibold text-[#0a192f] mb-1">
                  Customer Type
                </label>
                <select
                  value={formData.role}
                  onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                  className="w-full px-4 py-2.5 bg-[#f8fafc] border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#0a192f] text-sm"
                >
                  <option value="New Customer">New Customer</option>
                  <option value="Regular Customer">Regular Customer</option>
                </select>
              </div>

              {/* Star Rating Selection */}
              <div>
                <label className="block text-sm font-semibold text-[#0a192f] mb-1">
                  Rating
                </label>
                <div className="flex gap-1 cursor-pointer">
                  {[1, 2, 3, 4, 5].map((star) => (
                    <Star
                      key={star}
                      size={24}
                      onClick={() => setFormData({ ...formData, rating: star })}
                      className={`${star <= formData.rating
                        ? "fill-amber-400 text-amber-400"
                        : "text-gray-300"
                        } transition-colors`}
                    />
                  ))}
                </div>
              </div>

              {/* Review Text Area */}
              <div>
                <label className="block text-sm font-semibold text-[#0a192f] mb-1">
                  Your Review
                </label>
                <textarea
                  required
                  rows={4}
                  placeholder="Tell us about your experience with Medzo..."
                  value={formData.comment}
                  onChange={(e) => setFormData({ ...formData, comment: e.target.value })}
                  className="w-full px-4 py-2.5 bg-[#f8fafc] border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#0a192f] text-sm resize-none"
                />
              </div>

              {/* Submit Button */}
              <button
                type="submit"
                disabled={isSubmitting}
                className="w-full gradient-btn text-white font-semibold py-3 rounded-lg transition-all text-sm mt-2 disabled:opacity-60"
              >
                {isSubmitting ? 'Saving…' : 'Submit Review'}
              </button>
            </form>

          </div>
        </div>
      )}
    </section>
  );
};

export default Reviews;

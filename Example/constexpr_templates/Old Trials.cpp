#include "stdafx.h"


//// Example program
//#include <iostream>
//#include <string>
//#include <utility>
//#include <array>
//#include <algorithm>
//#include <typeinfo>
//#include <tuple>
//#include <utility>
//
//
//constexpr char to_lower_helper_char(const char c) {
//	return (c >= 'A' && c <= 'Z') ? c + ('a' - 'A') : c;
//}
//
//template <unsigned N, typename T, T... Nums>
//constexpr const std::array<char, N> to_lower_helper(const char(&str)[N], std::integer_sequence<T, Nums...>) {
//	return { to_lower_helper_char(str[Nums])... };
//}
//
//template <unsigned N>
//constexpr const std::array<char, N> to_lower(const char(&str)[N]) {
//	//std::cout << __PRETTY_FUNCTION__ << "\n";    
//	return to_lower_helper(str, std::make_integer_sequence<unsigned, N>());
//}
//
//
////------------------------------------------
//
//
//template <std::size_t ... nbrOfChars>
//constexpr const std::array<char, 6> left_helper(const char * str, std::index_sequence<nbrOfChars...>)
//{
//	std::array<char, 6> a2 = { nbrOfChars... };
//	return a2;
//}
//
////template <unsigned N>
////constexpr const std::array<char, N> left(const char (&str)[N], std::size_t nbrOfChars)
////{
////    return left_helper(str, std::make_integer_sequence<N>());
////}
//
//
//namespace detail {
//	template <std::size_t ... indices>
//	decltype(auto) build_string(const char * str, std::index_sequence<indices...>) {
//		return std::make_tuple(str[indices]...);
//	}
//}
//
//template <std::size_t N>
//constexpr decltype(auto) make_string(const char(&str)[N]) {
//	return detail::build_string(str, std::make_index_sequence<N>());
//}
//
//
////------------------------------------------
//
//
////constexpr char to_lower_helper_char(const char c) {
////    return (c >= 'A' && c <= 'Z') ? c + ('a' - 'A') : c;
////}
//
//template <unsigned N, typename T, T... Nums>
//constexpr const std::array<char, N> right_helper(const char(&str)[N], std::integer_sequence<T, Nums...>) {
//	return { to_lower_helper_char(str[Nums])... };
//}
//
//template <unsigned N>
//constexpr const std::array<char, N> right(const char(&str)[N]) {
//	return right_helper(str, std::make_integer_sequence<unsigned, N>());
//}
//
//
////------------------------------------------
//
///*
//template <std::size_t ... nbrOfChars>
//constexpr const std::array<char, 6> short_arr(const char * str, std::index_sequence<nbrOfChars...>)
//{
//std::array<char, 6> a2 = {nbrOfChars...};
//return a2;
//}
//*/
//
//template <unsigned N>
//constexpr const std::array<char, 6> short_arr_old(const char(&str)[N])
//{
//	return std::array<char, 6>{'T', 'T', str[3], 'R', 'R', 'R'};
//}
//
//template <unsigned N, unsigned L>
//constexpr const std::array<char, L> short_arr_old2(const char(&str)[N], const int& len)
//{
//	return std::array<char, L>{'T', 'T', str[3], 'R', 'R', 'R'};
//}
//
//
//template <unsigned N>
//constexpr const std::array<char, N> short_arr_old3(const char(&str)[N], const int& len)
//{
//	return std::array<char, N>{'T', 'T', str[3], 'R', 'R', 'R'};
//}
//
///*
//template <unsigned N, unsigned L>
//constexpr const std::array<char, L> short_arr_old4(const char (&str)[N], const int (& len)[L])
//{
//return std::array<char, L>{'T', 'T', str[3], 'R', 'R', 'R'};
//}
//*/
//
//template <std::size_t N, typename T, T... Nums>
//constexpr const std::array<char, N> short_arr_old4_helper(const char(&str)[N], std::integer_sequence<T, Nums...>) {
//	return { to_lower_helper_char(str[Nums])... };
//}
//
//template <std::size_t N>
//constexpr const std::array<char, N> short_arr_old4(const char(&str)[N], const unsigned int len)
//{
//	return short_arr_old4_helper(str, std::make_integer_sequence<unsigned int, 3>());
//}
//
//
//int main()
//{
//	constexpr auto arr = to_lower("TEST");
//	static_assert(arr[0] == 't', "ERROR");
//	static_assert(arr[1] == 'e', "ERROR");
//	static_assert(arr[2] == 's', "ERROR");
//	static_assert(arr[3] == 't', "ERROR");
//
//	//constexpr auto arr2 = right("ABCD.EFGHI");
//	constexpr const std::array<char, 11> arr2 = right("ABCD.EFGHI");
//	//std::cout << typeid(arr2).name() << std::endl;
//	std::cout << std::string(arr2.data()) << std::endl;
//
//	constexpr char arr3[] = "TYUIO";
//	std::cout << arr3 << std::endl;
//
//	//auto it = std::find(arr2.begin(), arr2.end(), '.');
//	std::array<char, 11>::const_iterator it = std::find(arr2.begin(), arr2.end(), '.');
//	//constexpr const std::array<char, arr2.size()> it = std::find(arr2.begin(), arr2.end(), '.');
//	//auto it = std::find(std::begin(arr3), std::end(arr3), '.');
//	if (it == std::end(arr3))
//		std::cout << "NOT FOUND" << std::endl;
//	else
//		std::cout << *it << std::endl;
//
//	auto HelloStrObject = make_string("hello");
//	HelloStrObject;
//
//	constexpr const std::array<char, 6> shold = short_arr_old("oooooooooo");
//	std::cout << std::string(shold.data()) << std::endl;
//
//	constexpr const std::array<char, 6> sh2 = short_arr_old2<6, 6>("ggggg", 6);
//	std::cout << std::string(sh2.data()) << std::endl;
//
//	constexpr const std::array<char, 6> sh3 = short_arr_old3("hhhhh", 6);
//	std::cout << std::string(sh3.data()) << std::endl;
//
//	constexpr const std::size_t len = 6;
//	constexpr const unsigned int lenI = 3;
//	constexpr const std::array<char, 6> sh4 = short_arr_old4("iiiii", lenI);
//	std::cout << "\"" << std::string(sh4.data()) << "\"" << std::endl;
//	std::cout << "sh4 size: " << sh4.size() << std::endl;
//}
#include "stdafx.h"

#include <iostream>
#include <string>
#include <utility>
#include <array>
#include <algorithm>
#include <typeinfo>
#include <tuple>
#include <utility>


constexpr char to_lower_helper_char(const char c) {
	return (c >= 'A' && c <= 'Z') ? c + ('a' - 'A') : c;
}

template <unsigned N, typename T, T... Nums>
constexpr const std::array<char, N> to_lower_helper(const char(&str)[N], std::integer_sequence<T, Nums...>) {
	return { to_lower_helper_char(str[Nums])... };
}

template <unsigned N>
constexpr const std::array<char, N> to_lower(const char(&str)[N]) {
	//std::cout << __PRETTY_FUNCTION__ << "\n";    
	return to_lower_helper(str, std::make_integer_sequence<unsigned, N>());
}

//-------------------------------------


template <unsigned N, typename T, T... Nums>
constexpr const std::array<char, N> left_helper(const char(&str)[N], const unsigned int len, std::integer_sequence<T, Nums...>) {
	return { ((Nums < len) ? str[Nums] : '\0')... };
}


template <unsigned N>
constexpr const std::array<char, N> left(const char(&str)[N], const unsigned int len) {
	//std::cout << __PRETTY_FUNCTION__ << "\n";  
	const unsigned int lenI = 3;

	return left_helper(str, len, std::make_integer_sequence<unsigned int, N>());
}


//-------------------------------------


template <unsigned N, typename T, T... Nums>
constexpr const std::array<char, N> right_helper(const char(&str)[N], const unsigned int start, std::integer_sequence<T, Nums...>) {
	return { (str[(Nums + start) % N])... };
}


template <unsigned N>
constexpr const std::array<char, N> right(const char(&str)[N], const unsigned int start) {
	return right_helper(str, start, std::make_integer_sequence<unsigned int, N>());
}


//-------------------------------------

constexpr std::array<int, 7> createA1(const unsigned int len)
{
	const unsigned int lenI = 7;
	return std::array<int, lenI>{ 1, 2, 3 };
}

void Left()
{
	constexpr auto arr = to_lower("TEST");
	static_assert(arr[0] == 't', "ERROR");
	static_assert(arr[1] == 'e', "ERROR");
	static_assert(arr[2] == 's', "ERROR");
	static_assert(arr[3] == 't', "ERROR");

	constexpr auto left1 = left("ssWWsssWWW", 4);

	//constexpr const std::size_t len = 6;
	//constexpr const unsigned int lenI = 3;
	//constexpr const std::array<char, 6> sh4 = short_arr_old4("iiiii", lenI);
	std::cout << "\"" << std::string(left1.data()) << "\"" << std::endl;
	std::cout << "left1 std::string length: " << std::string(left1.data()).length() << std::endl;
	std::cout << "left1 size: " << left1.size() << std::endl;


	const unsigned int lenI = 3;
	auto seq1 = std::make_integer_sequence<unsigned int, lenI>();

	auto a1 = createA1(7);


	constexpr auto right1 = right("123456789", 4);
	std::cout << "\"" << std::string(right1.data()) << "\"" << std::endl;
	std::cout << "right1 std::string length: " << std::string(right1.data()).length() << std::endl;
	std::cout << "right1 size: " << right1.size() << std::endl;
}
cmake_minimum_required(VERSION 3.22)
project(aura_llama_bridge LANGUAGES CXX)

if(NOT DEFINED LLAMA_CPP_ROOT)
    message(FATAL_ERROR "LLAMA_CPP_ROOT must point to the llama.cpp checkout")
endif()

add_subdirectory("${LLAMA_CPP_ROOT}" "${CMAKE_BINARY_DIR}/llama.cpp" EXCLUDE_FROM_ALL)

add_library(aura_llama SHARED aura_llama_bridge.cpp)
target_compile_features(aura_llama PRIVATE cxx_std_17)
target_include_directories(aura_llama PRIVATE "${LLAMA_CPP_ROOT}/include")
target_link_libraries(aura_llama PRIVATE llama)

set_target_properties(aura_llama PROPERTIES
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN YES
)

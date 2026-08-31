# Install script for directory: C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/libfreerdp

# Set the install prefix
if(NOT DEFINED CMAKE_INSTALL_PREFIX)
  set(CMAKE_INSTALL_PREFIX "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/Compiled")
endif()
string(REGEX REPLACE "/$" "" CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}")

# Set the install configuration name.
if(NOT DEFINED CMAKE_INSTALL_CONFIG_NAME)
  if(BUILD_TYPE)
    string(REGEX REPLACE "^[^A-Za-z0-9_]+" ""
           CMAKE_INSTALL_CONFIG_NAME "${BUILD_TYPE}")
  else()
    set(CMAKE_INSTALL_CONFIG_NAME "Release")
  endif()
  message(STATUS "Install configuration: \"${CMAKE_INSTALL_CONFIG_NAME}\"")
endif()

# Set the component getting installed.
if(NOT CMAKE_INSTALL_COMPONENT)
  if(COMPONENT)
    message(STATUS "Install component: \"${COMPONENT}\"")
    set(CMAKE_INSTALL_COMPONENT "${COMPONENT}")
  else()
    set(CMAKE_INSTALL_COMPONENT)
  endif()
endif()

# Is this installation the result of a crosscompile?
if(NOT DEFINED CMAKE_CROSSCOMPILING)
  set(CMAKE_CROSSCOMPILING "FALSE")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "libraries" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/Release/freerdp3.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/Debug/freerdp3.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/MinSizeRel/freerdp3.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/RelWithDebInfo/freerdp3.lib")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "libraries" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/Release/freerdp3.dll")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/Debug/freerdp3.dll")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/MinSizeRel/freerdp3.dll")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/RelWithDebInfo/freerdp3.dll")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/pkgconfig" TYPE FILE FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/freerdp3.pc")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP3" TYPE FILE FILES
    "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/FreeRDPConfig.cmake"
    "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/FreeRDPConfigVersion.cmake"
    )
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  # Include the install script for each subdirectory.
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/emu/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/utils/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/common/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/gdi/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/cache/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/crypto/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/locale/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/core/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/codec/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/primitives/cmake_install.cmake")

endif()

string(REPLACE ";" "\n" CMAKE_INSTALL_MANIFEST_CONTENT
       "${CMAKE_INSTALL_MANIFEST_FILES}")
if(CMAKE_INSTALL_LOCAL_ONLY)
  file(WRITE "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/libfreerdp/install_local_manifest.txt"
     "${CMAKE_INSTALL_MANIFEST_CONTENT}")
endif()

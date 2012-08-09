/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#pragma once

#ifndef __MEDIA_HEADER_BOX_DEFINED
#define __MEDIA_HEADER_BOX_DEFINED

#include "FullBox.h"

#define MEDIA_HEADER_BOX_TYPE                                                 L"mdhd"

class CMediaHeaderBox :
  public CFullBox
{
public:
  // initializes a new instance of CMediaHeaderBox class
  CMediaHeaderBox(void);

  // destructor
  virtual ~CMediaHeaderBox(void);

  /* get methods */

  // gets whole box into buffer
  // @param buffer : the buffer for box data
  // @param length : the length of buffer for data
  // @return : true if all data were successfully stored into buffer, false otherwise
  virtual bool GetBox(uint8_t **buffer, uint32_t *length);

  // gets the creation time of the media in this track (in seconds since midnight, Jan. 1, 1904, in UTC time)
  // @return : the creation time of the media in this track (in seconds since midnight, Jan. 1, 1904, in UTC time)
  virtual uint64_t GetCreationTime(void);

  // gets the most recent time the media in this track was modified (in seconds since midnight, Jan. 1, 1904, in UTC time)
  // @return : the most recent time the media in this track was modified (in seconds since midnight, Jan. 1, 1904, in UTC time)
  virtual uint64_t GetModificationTime(void);

  // gets the time-scale for this media; this is the number of time units that pass in one second
  // for example, a time coordinate system that measures time in sixtieths of a second has a time scale of 60
  // @return : the time-scale for this media
  virtual uint32_t GetTimeScale(void);

  // gets the duration of this media (in the scale of the timescale)
  // @return : the duration of this media 
  virtual uint64_t GetDuration(void);

  // gets the language code for this media
  // see ISO 639-2/T for the set of three character codes
  // @return : the language code for this media
  virtual const wchar_t *GetLanguage(void);

  /* set methods */

  /* other methods */

  // parses data in buffer
  // @param buffer : buffer with box data for parsing
  // @param length : the length of data in buffer
  // @return : true if parsed successfully, false otherwise
  virtual bool Parse(const uint8_t *buffer, uint32_t length);

  // gets box data in human readable format
  // @param indent : string to insert before each line
  // @return : box data in human readable format or NULL if error
  virtual wchar_t *GetParsedHumanReadable(const wchar_t *indent);

protected:

  // the creation time of the media in this track (in seconds since midnight, Jan. 1, 1904, in UTC time)
  uint64_t creationTime;
  // the most recent time the media in this track was modified (in seconds since midnight, Jan. 1, 1904, in UTC time)
  uint64_t modificationTime;
  // specifies the time-scale for this media; this is the number of time units that pass in one second
  // for example, a time coordinate system that measures time in sixtieths of a second has a time scale of 60
  uint32_t timeScale;
  // the duration of this media (in the scale of the timescale)
  uint64_t duration;
  // the language code for this media
  // see ISO 639-2/T for the set of three character codes
  wchar_t *language;

  // gets box size added to size
  // method is called to determine whole box size for storing box into buffer
  // @param size : the size of box calling this method
  // @return : size of box 
  virtual uint64_t GetBoxSize(uint64_t size);

  // parses data in buffer
  // @param buffer : buffer with box data for parsing
  // @param length : the length of data in buffer
  // @param processAdditionalBoxes : specifies if additional boxes have to be processed
  // @return : true if parsed successfully, false otherwise
  virtual bool ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes);
};

#endif
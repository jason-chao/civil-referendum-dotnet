# 2014 Macau Civil Referendum

The source code of the software system developed for the 2014 Civil Referendum on Chief Executive Election in Macau is published here for archival purpose.    In the case which a vote anticipates authorities' interference, new techniques should be adopted to anonymise and protect voters' data.  The use of the same design and architecture in future votes is strongly discouraged.

Directory

- ReferendumData
  - The data model
  - Key logic for voter validation and vote processing
- ReferendumWeb
  - Web application for the online voting channel
- ReferendumStatistics
  - Web application for publishing the voter turnout and the results
- SMSProxy
  - HTTP interface between ReferendumWeb and the carrier for sending SMS verification codes 

More information about the vote [https://macau2014.openmacau.org/](https://macau2014.openmacau.org/)
